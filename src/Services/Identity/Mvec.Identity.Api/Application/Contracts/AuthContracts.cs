using Mvec.Identity.Api.Domain;

namespace Mvec.Identity.Api.Application.Contracts;

// ---- Requests ----

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserType UserType,
    string? PhoneNumber = null);

public sealed record LoginRequest(string Email, string Password);

public sealed record TwoFactorLoginRequest(string ChallengeToken, string Code);

public sealed record RefreshRequest(string? RefreshToken);

public sealed record SocialLoginRequest(string IdToken, UserType UserType = UserType.Buyer);

public sealed record VerifyEmailRequest(string Email, string Code);

// ---- Responses ----

/// <summary>Issued on a completed login / refresh. The refresh token is also set as an httpOnly cookie.</summary>
public sealed record AuthTokens(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    string TokenType = "Bearer");

/// <summary>
/// Login outcome. Either <see cref="Tokens"/> is populated (success) or
/// <see cref="TwoFactorRequired"/> is true with a <see cref="ChallengeToken"/> to complete via /login/2fa.
/// </summary>
public sealed record LoginResponse(
    AuthTokens? Tokens,
    bool TwoFactorRequired,
    string? ChallengeToken);
