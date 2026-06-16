namespace Mvec.Vendor.Api.Domain;

/// <summary>
/// A vendor payout bank account (vnd.VendorBankAccounts). The account number is stored encrypted
/// (<see cref="AccountNumberEnc"/>, VARBINARY) — never in plain text. Child of the Vendor aggregate.
/// </summary>
public class VendorBankAccount
{
    public long Id { get; private set; }
    public long VendorId { get; private set; }
    public string AccountHolder { get; private set; } = string.Empty;
    public string BankName { get; private set; } = string.Empty;
    public byte[] AccountNumberEnc { get; private set; } = Array.Empty<byte>();
    public string? Ifsc { get; private set; }
    public bool IsPrimary { get; private set; } = true;
    public DateTime CreatedUtc { get; private set; } = DateTime.UtcNow;

    private VendorBankAccount() { }

    public static VendorBankAccount Create(long vendorId, string accountHolder, string bankName,
        byte[] accountNumberEnc, string? ifsc, bool isPrimary) => new()
    {
        VendorId = vendorId,
        AccountHolder = accountHolder.Trim(),
        BankName = bankName.Trim(),
        AccountNumberEnc = accountNumberEnc,
        Ifsc = ifsc?.Trim().ToUpperInvariant(),
        IsPrimary = isPrimary
    };
}
