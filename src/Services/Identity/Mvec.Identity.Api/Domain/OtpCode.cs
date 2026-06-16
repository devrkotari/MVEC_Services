namespace Mvec.Identity.Api.Domain;

/// <summary>
/// Short-lived one-time code mapped to <c>idn.OtpCodes</c>. Only the hash of the code is stored
/// (VARBINARY); codes are single-use (ConsumedUtc set on use).
/// </summary>
public class OtpCode
{
    public long Id { get; private set; }
    public long? UserId { get; private set; }
    public OtpPurpose Purpose { get; private set; }
    public byte[] CodeHash { get; private set; } = Array.Empty<byte>();
    public DateTime ExpiresUtc { get; private set; }
    public DateTime? ConsumedUtc { get; private set; }
    public DateTime CreatedUtc { get; private set; } = DateTime.UtcNow;

    private OtpCode() { }

    public static OtpCode Create(long userId, byte[] codeHash, OtpPurpose purpose, DateTime expiresUtc) => new()
    {
        UserId = userId,
        CodeHash = codeHash,
        Purpose = purpose,
        ExpiresUtc = expiresUtc
    };

    public bool IsExpired => DateTime.UtcNow >= ExpiresUtc;
    public bool IsUsable => ConsumedUtc is null && !IsExpired;

    public void Consume() => ConsumedUtc = DateTime.UtcNow;
}
