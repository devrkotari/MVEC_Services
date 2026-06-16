using Mvec.Identity.Api.Domain;

namespace Mvec.Identity.Api.Application.Abstractions;

/// <summary>Result of issuing a fresh refresh token: the raw value (returned once) and its stored hash.</summary>
public readonly record struct RefreshTokenPair(string RawToken, byte[] TokenHash, DateTime ExpiresAt);

public interface IJwtTokenService
{
    /// <summary>Mints a signed, short-lived access token for the given user.</summary>
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);

    /// <summary>Generates a cryptographically random refresh token plus its storable hash.</summary>
    RefreshTokenPair CreateRefreshToken();

    /// <summary>Mints a short-lived token proving the password step passed, pending 2FA completion.</summary>
    string CreateTwoFactorChallenge(User user);

    /// <summary>Validates a 2FA challenge token and returns the user id it was issued for, or null.</summary>
    long? ValidateTwoFactorChallenge(string challengeToken);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>One-way hash for high-entropy secrets (refresh tokens, OTP codes), stored as VARBINARY.</summary>
public interface ITokenHasher
{
    byte[] Hash(string raw);
}

public interface ITotpService
{
    string GenerateSecret();
    bool Verify(string base32Secret, string code);
    string BuildProvisioningUri(string base32Secret, string accountName, string issuer);
}

/// <summary>Accessor for the authenticated principal on the current request.</summary>
public interface ICurrentUser
{
    long? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
