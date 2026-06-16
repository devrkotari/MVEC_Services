namespace Mvec.Vendor.Api.Domain;

/// <summary>An immutable audit row of an admin KYC decision (vnd.KycReviewLog). Child of the Vendor aggregate.</summary>
public class KycReviewLog
{
    public long Id { get; private set; }
    public long VendorId { get; private set; }
    public KycDecision Decision { get; private set; }
    public string? Reason { get; private set; }
    public long ReviewedBy { get; private set; }
    public DateTime ReviewedUtc { get; private set; } = DateTime.UtcNow;

    private KycReviewLog() { }

    public static KycReviewLog Record(long vendorId, KycDecision decision, string? reason, long reviewedBy) => new()
    {
        VendorId = vendorId,
        Decision = decision,
        Reason = reason,
        ReviewedBy = reviewedBy
    };
}
