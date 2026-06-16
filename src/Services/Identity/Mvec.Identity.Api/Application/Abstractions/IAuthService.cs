using Mvec.BuildingBlocks.Common;
using Mvec.Identity.Api.Application.Contracts;

namespace Mvec.Identity.Api.Application.Abstractions;

/// <summary>
/// Authentication/authorization use cases: registration, login (incl. admin 2FA),
/// refresh-token rotation, logout, social login and email verification.
/// </summary>
public interface IAuthService
{
    /// <summary>Buyer/vendor self-registration. Publishes <c>UserRegistered</c>.</summary>
    Task<Result<UserDto>> RegisterAsync(RegisterRequest req, CancellationToken ct = default);

    /// <summary>Password login. Returns tokens, or a 2FA challenge when enabled.</summary>
    Task<Result<LoginResponse>> LoginAsync(LoginRequest req, CancellationToken ct = default);

    /// <summary>Completes a login by verifying the TOTP code against an issued 2FA challenge.</summary>
    Task<Result<LoginResponse>> CompleteTwoFactorAsync(TwoFactorLoginRequest req, CancellationToken ct = default);

    /// <summary>Rotates tokens: validates the refresh token, revokes it and issues a fresh pair.</summary>
    Task<Result<AuthTokens>> RefreshAsync(string? rawRefreshToken, CancellationToken ct = default);

    /// <summary>Revokes the supplied refresh token for the given user.</summary>
    Task<Result> LogoutAsync(long userId, string? rawRefreshToken, CancellationToken ct = default);

    /// <summary>Validates a social provider id-token, find-or-creates the user and issues tokens.</summary>
    Task<Result<AuthTokens>> SocialLoginAsync(string provider, SocialLoginRequest req, CancellationToken ct = default);

    /// <summary>Confirms a user's email against a one-time verification code.</summary>
    Task<Result> VerifyEmailAsync(VerifyEmailRequest req, CancellationToken ct = default);
}
