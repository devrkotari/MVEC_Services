using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Mvec.Identity.Api.Application.Abstractions;
using Mvec.Identity.Api.Application.Options;
using Mvec.Identity.Api.Domain;

namespace Mvec.Identity.Api.Infrastructure.Security;

/// <summary>
/// Issues signed access tokens (15 min), random rotating refresh tokens (7 days, stored as a SHA-256
/// hash), and short-lived 2FA challenge tokens. Symmetric HS256 signing key from config/Key Vault.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    public const string TwoFactorPurpose = "2fa_challenge";
    private const int TwoFactorChallengeMinutes = 5;
    private const int RefreshTokenBytes = 32; // 256-bit

    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;
    private readonly TokenValidationParameters _challengeValidation;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        _challengeValidation = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }

    public (string Token, DateTime ExpiresAt) CreateAccessToken(User user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        // Authorization is driven by UserType (Buyer/Vendor/Admin), surfaced as the "role" claim.
        var role = user.UserType.ToString();
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [JwtRegisteredClaimNames.Email] = user.Email,
            [ClaimTypes.Role] = role,
            ["role"] = role
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Claims = claims,
            SigningCredentials = _credentials
        };

        return (_handler.CreateToken(descriptor), expires);
    }

    public RefreshTokenPair CreateRefreshToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var expiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenDays);
        return new RefreshTokenPair(raw, hash, expiresAt);
    }

    public string CreateTwoFactorChallenge(User user)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(TwoFactorChallengeMinutes),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                ["purpose"] = TwoFactorPurpose
            },
            SigningCredentials = _credentials
        };
        return _handler.CreateToken(descriptor);
    }

    public long? ValidateTwoFactorChallenge(string challengeToken)
    {
        var result = _handler.ValidateTokenAsync(challengeToken, _challengeValidation).GetAwaiter().GetResult();
        if (!result.IsValid) return null;

        if (!result.Claims.TryGetValue("purpose", out var purpose) || purpose as string != TwoFactorPurpose)
            return null;

        if (result.Claims.TryGetValue(JwtRegisteredClaimNames.Sub, out var sub) &&
            long.TryParse(sub as string, out var userId))
            return userId;

        return null;
    }
}
