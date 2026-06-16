using Microsoft.Extensions.Logging;
using Mvec.BuildingBlocks.Common;
using Mvec.BuildingBlocks.Persistence;
using Mvec.Contracts.Events;
using Mvec.Identity.Api.Application.Abstractions;
using Mvec.Identity.Api.Application.Contracts;
using Mvec.Identity.Api.Domain;

namespace Mvec.Identity.Api.Application.Services;

/// <summary>
/// Orchestrates registration, login (incl. admin 2FA), token refresh/rotation, logout, social login
/// and email verification. All persistence flows through repositories committed via <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IExternalLoginRepository _externalLogins;
    private readonly IOtpRepository _otps;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenHasher _tokenHasher;
    private readonly IJwtTokenService _jwt;
    private readonly ITotpService _totp;
    private readonly IEventPublisher _events;
    private readonly IEnumerable<IExternalAuthValidator> _externalValidators;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IRoleRepository roles,
        IRefreshTokenRepository refreshTokens,
        IExternalLoginRepository externalLogins,
        IOtpRepository otps,
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        ITokenHasher tokenHasher,
        IJwtTokenService jwt,
        ITotpService totp,
        IEventPublisher events,
        IEnumerable<IExternalAuthValidator> externalValidators,
        ILogger<AuthService> logger)
    {
        _users = users;
        _roles = roles;
        _refreshTokens = refreshTokens;
        _externalLogins = externalLogins;
        _otps = otps;
        _uow = uow;
        _passwordHasher = passwordHasher;
        _tokenHasher = tokenHasher;
        _jwt = jwt;
        _totp = totp;
        _events = events;
        _externalValidators = externalValidators;
        _logger = logger;
    }

    public async Task<Result<UserDto>> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        // Self-registration is for buyers/vendors only; admins are seeded (SCR-A01).
        if (req.UserType == UserType.Admin)
            return Result<UserDto>.Failure(Error.Validation("Admin accounts cannot be self-registered."));

        var email = req.Email.Trim().ToLowerInvariant();
        if (await _users.EmailExistsAsync(email, ct))
            return Result<UserDto>.Failure(Error.Conflict("An account with this email already exists."));

        var user = User.CreateLocal(
            email, _passwordHasher.Hash(req.Password), req.FirstName, req.LastName, req.UserType, req.PhoneNumber);
        await AssignDefaultRoleAsync(user, ct);
        _users.Add(user);

        await _events.PublishAsync(
            new UserRegistered(user.Id, user.Email, user.UserType.ToString(), DateTime.UtcNow), ct);

        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Registered user {UserId} ({UserType})", user.Id, user.UserType);

        return Result<UserDto>.Success(user.ToDto());
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(req.Email, ct: ct);

        // Uniform failure to avoid leaking which accounts exist.
        if (user is null || !user.HasPassword || !_passwordHasher.Verify(req.Password, user.PasswordHash!))
            return Result<LoginResponse>.Failure(Error.Validation("Invalid email or password."));

        if (!user.IsActive)
            return Result<LoginResponse>.Failure(AccountSuspended());

        // OTP / admin 2FA is currently disabled: every valid login is completed directly.
        // (Re-enable by restoring the user.TwoFactorEnabled challenge branch here.)

        var tokens = await IssueTokensAsync(user, ct);
        return Result<LoginResponse>.Success(new LoginResponse(tokens, false, null));
    }

    public async Task<Result<LoginResponse>> CompleteTwoFactorAsync(TwoFactorLoginRequest req, CancellationToken ct = default)
    {
        var userId = _jwt.ValidateTwoFactorChallenge(req.ChallengeToken);
        if (userId is null)
            return Result<LoginResponse>.Failure(Error.Validation("Invalid or expired 2FA challenge."));

        var user = await _users.GetByIdAsync(userId.Value, ct);
        if (user is null || !user.IsActive || !user.TwoFactorEnabled || user.TwoFactorSecret is null)
            return Result<LoginResponse>.Failure(Error.Validation("2FA is not available for this account."));

        if (!_totp.Verify(user.TwoFactorSecret, req.Code))
            return Result<LoginResponse>.Failure(Error.Validation("Invalid authentication code."));

        var tokens = await IssueTokensAsync(user, ct);
        return Result<LoginResponse>.Success(new LoginResponse(tokens, false, null));
    }

    public async Task<Result<AuthTokens>> RefreshAsync(string? rawRefreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
            return Result<AuthTokens>.Failure(Error.Validation("Missing refresh token."));

        var hash = _tokenHasher.Hash(rawRefreshToken);
        var token = await _refreshTokens.GetByHashAsync(hash, ct);

        if (token is null)
            return Result<AuthTokens>.Failure(Error.Validation("Invalid refresh token."));

        var user = await _users.GetByIdAsync(token.UserId, ct);

        // Reusing an already-revoked/expired token is a strong signal of theft — reject hard.
        if (!token.IsActive || user is null)
        {
            _logger.LogWarning("Rejected refresh attempt with inactive token {TokenId}", token.Id);
            return Result<AuthTokens>.Failure(Error.Validation("Invalid refresh token."));
        }

        if (!user.IsActive)
            return Result<AuthTokens>.Failure(Error.Validation("Account is suspended."));

        // Rotate: revoke the presented token and issue a fresh pair (FR-002).
        var pair = _jwt.CreateRefreshToken();
        var replacement = user.IssueRefreshToken(pair.TokenHash, pair.ExpiresAt);
        _refreshTokens.Add(replacement);
        token.Revoke(Convert.ToHexString(pair.TokenHash));

        var (access, accessExp) = _jwt.CreateAccessToken(user);
        await _uow.SaveChangesAsync(ct);

        return Result<AuthTokens>.Success(new AuthTokens(access, accessExp, pair.RawToken, pair.ExpiresAt));
    }

    public async Task<Result> LogoutAsync(long userId, string? rawRefreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
            return Result.Success(); // nothing to revoke

        var hash = _tokenHasher.Hash(rawRefreshToken);
        var token = await _refreshTokens.GetByHashForUserAsync(hash, userId, ct);

        if (token is { IsActive: true })
        {
            token.Revoke();
            await _uow.SaveChangesAsync(ct);
        }
        return Result.Success();
    }

    public async Task<Result<AuthTokens>> SocialLoginAsync(string provider, SocialLoginRequest req, CancellationToken ct = default)
    {
        var validator = _externalValidators.FirstOrDefault(
            v => string.Equals(v.Provider, provider, StringComparison.OrdinalIgnoreCase));
        if (validator is null)
            return Result<AuthTokens>.Failure(Error.Validation($"Unsupported provider '{provider}'."));

        var info = await validator.ValidateAsync(req.IdToken, ct);
        if (info is null)
            return Result<AuthTokens>.Failure(Error.Validation("Could not validate the social login token."));

        if (req.UserType == UserType.Admin)
            return Result<AuthTokens>.Failure(Error.Validation("Admin accounts cannot be created via social login."));

        var user = await _users.GetByEmailAsync(info.Email, includeExternalLogins: true, ct);

        if (user is null)
        {
            var email = info.Email.Trim().ToLowerInvariant();
            var (first, last) = SplitName(info.DisplayName);
            user = User.CreateExternal(email, first, last, req.UserType, info.Provider, info.ProviderKey);
            await AssignDefaultRoleAsync(user, ct);
            _users.Add(user);
            await _events.PublishAsync(
                new UserRegistered(user.Id, user.Email, user.UserType.ToString(), DateTime.UtcNow), ct);
        }
        else
        {
            if (!user.IsActive)
                return Result<AuthTokens>.Failure(Error.Validation("Account is suspended."));
            var login = user.AddExternalLogin(info.Provider, info.ProviderKey);
            if (login is not null) _externalLogins.Add(login);
        }

        var tokens = await IssueTokensAsync(user, ct);
        return Result<AuthTokens>.Success(tokens);
    }

    public async Task<Result> VerifyEmailAsync(VerifyEmailRequest req, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(req.Email, ct: ct);
        if (user is null)
            return Result.Failure(Error.Validation("Invalid verification request."));

        if (user.EmailConfirmed)
            return Result.Success();

        var codeHash = _tokenHasher.Hash(req.Code);
        var otp = await _otps.FindAsync(user.Id, OtpPurpose.EmailVerify, codeHash, ct);

        if (otp is null || !otp.IsUsable)
            return Result.Failure(Error.Validation("Invalid or expired verification code."));

        otp.Consume();
        user.ConfirmEmail();
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Assigns the role whose name matches the user's <see cref="UserType"/>, if it exists.</summary>
    private async Task AssignDefaultRoleAsync(User user, CancellationToken ct)
    {
        var role = await _roles.GetByNameAsync(user.UserType.ToString(), ct);
        if (role is not null) user.AssignRole(role.Id);
        else _logger.LogWarning("Role '{Role}' not found; user created without a role assignment.", user.UserType);
    }

    /// <summary>Mints an access token + a fresh rotating refresh token and persists the latter.</summary>
    private async Task<AuthTokens> IssueTokensAsync(User user, CancellationToken ct)
    {
        var (access, accessExp) = _jwt.CreateAccessToken(user);
        var pair = _jwt.CreateRefreshToken();
        var refresh = user.IssueRefreshToken(pair.TokenHash, pair.ExpiresAt);
        _refreshTokens.Add(refresh);
        user.RecordLogin();
        await _uow.SaveChangesAsync(ct);
        return new AuthTokens(access, accessExp, pair.RawToken, pair.ExpiresAt);
    }

    private static (string First, string Last) SplitName(string displayName)
    {
        var parts = displayName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => ("User", string.Empty),
            1 => (parts[0], string.Empty),
            _ => (parts[0], parts[1])
        };
    }

    private static Error AccountSuspended() =>
        new("account_suspended", "This account has been suspended.");
}
