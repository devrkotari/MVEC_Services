namespace Mvec.Identity.Api.Domain;

/// <summary>
/// Rotating refresh token mapped to <c>idn.RefreshTokens</c>. Only the SHA-256 hash of the raw
/// token is persisted (VARBINARY). On each use the token is revoked and replaced (FR-002 rotation).
/// </summary>
public class RefreshToken
{
    public long Id { get; private set; }
    public long UserId { get; private set; }
    public byte[] TokenHash { get; private set; } = Array.Empty<byte>();
    public Guid? JwtId { get; private set; }
    public DateTime ExpiresUtc { get; private set; }
    public DateTime CreatedUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? RevokedUtc { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? ReplacedByToken { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Issue(long userId, byte[] tokenHash, DateTime expiresUtc) => new()
    {
        UserId = userId,
        TokenHash = tokenHash,
        ExpiresUtc = expiresUtc
    };

    public bool IsExpired => DateTime.UtcNow >= ExpiresUtc;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string? replacedByToken = null)
    {
        if (IsRevoked) return;
        IsRevoked = true;
        RevokedUtc = DateTime.UtcNow;
        ReplacedByToken = replacedByToken;
    }
}
