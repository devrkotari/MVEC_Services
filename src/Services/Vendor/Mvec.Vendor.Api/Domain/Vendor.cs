namespace Mvec.Vendor.Api.Domain;

/// <summary>
/// Aggregate root mapped to <c>vnd.Vendors</c>. Keys are database-generated (BIGINT IDENTITY).
/// Owns its KYC documents, bank accounts and admin review log. Implements the WF-002 onboarding
/// state machine (Pending → UnderReview → Approved/Rejected) and the vendor lifecycle (FR-016).
/// </summary>
public class Vendor
{
    public long Id { get; private set; }
    public long OwnerUserId { get; private set; }
    public string BusinessName { get; private set; } = string.Empty;
    public string? BusinessType { get; private set; }
    public string ContactEmail { get; private set; } = string.Empty;
    public string? ContactPhone { get; private set; }
    public string? Pan { get; private set; }
    public string? Gstin { get; private set; }
    public KycStatus KycStatus { get; private set; } = KycStatus.Pending;
    public VendorTier Tier { get; private set; } = VendorTier.Free;
    public int ProductLimit { get; private set; } = 50;
    public decimal CommissionPct { get; private set; } = 10.00m;
    public VendorStatus Status { get; private set; } = VendorStatus.Pending;
    public decimal RatingAvg { get; private set; }
    public int FulfilledOrders { get; private set; }
    public DateTime MemberSinceUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; private set; }

    private readonly List<VendorKycDocument> _kycDocuments = new();
    public IReadOnlyCollection<VendorKycDocument> KycDocuments => _kycDocuments.AsReadOnly();

    private readonly List<KycReviewLog> _reviewLogs = new();
    public IReadOnlyCollection<KycReviewLog> ReviewLogs => _reviewLogs.AsReadOnly();

    private readonly List<VendorBankAccount> _bankAccounts = new();
    public IReadOnlyCollection<VendorBankAccount> BankAccounts => _bankAccounts.AsReadOnly();

    /// <summary>Documents that must all be present before KYC can move to <see cref="KycStatus.UnderReview"/>.</summary>
    public static readonly IReadOnlyList<KycDocType> RequiredKycDocs =
        new[] { KycDocType.NationalId, KycDocType.Bank, KycDocType.AddressProof };

    private Vendor() { }

    public static Vendor Register(long ownerUserId, string businessName, string? businessType,
        string contactEmail, string? contactPhone, string? pan, string? gstin) => new()
    {
        OwnerUserId = ownerUserId,
        BusinessName = businessName.Trim(),
        BusinessType = businessType?.Trim(),
        ContactEmail = contactEmail.Trim().ToLowerInvariant(),
        ContactPhone = contactPhone,
        Pan = pan?.Trim().ToUpperInvariant(),
        Gstin = gstin?.Trim().ToUpperInvariant(),
        KycStatus = KycStatus.Pending,
        Status = VendorStatus.Pending
    };

    /// <summary>BR-001: a vendor may not list products until KYC is approved and the account is active.</summary>
    public bool IsApproved => KycStatus == KycStatus.Approved && Status == VendorStatus.Active;

    /// <summary>
    /// Adds a KYC document, replacing any existing document of the same type (re-upload to fix a
    /// rejected/incorrect file). Once all <see cref="RequiredKycDocs"/> are present the vendor
    /// auto-advances from Pending to UnderReview.
    /// </summary>
    public VendorKycDocument SubmitKycDocument(KycDocType docType, string blobUrl)
    {
        var existing = _kycDocuments.FirstOrDefault(d => d.DocType == docType);
        if (existing is not null)
        {
            existing.Replace(blobUrl);
            AdvanceToUnderReviewIfReady();
            Touch();
            return existing;
        }

        var doc = VendorKycDocument.Create(Id, docType, blobUrl);
        _kycDocuments.Add(doc);
        AdvanceToUnderReviewIfReady();
        Touch();
        return doc;
    }

    private void AdvanceToUnderReviewIfReady()
    {
        if (KycStatus != KycStatus.Pending) return;
        var present = _kycDocuments.Select(d => d.DocType).ToHashSet();
        if (RequiredKycDocs.All(present.Contains))
            KycStatus = KycStatus.UnderReview;
    }

    /// <summary>Admin approves KYC (FR-012): KYC → Approved, account → Active, logged in vnd.KycReviewLog.</summary>
    public KycReviewLog Approve(long adminId)
    {
        KycStatus = KycStatus.Approved;
        Status = VendorStatus.Active;
        foreach (var doc in _kycDocuments) doc.MarkApproved();
        var log = KycReviewLog.Record(Id, KycDecision.Approved, null, adminId);
        _reviewLogs.Add(log);
        Touch();
        return log;
    }

    /// <summary>Admin rejects KYC (FR-012). A reason is mandatory and is carried on the event.</summary>
    public KycReviewLog Reject(long adminId, string reason)
    {
        KycStatus = KycStatus.Rejected;
        var log = KycReviewLog.Record(Id, KycDecision.Rejected, reason, adminId);
        _reviewLogs.Add(log);
        Touch();
        return log;
    }

    /// <summary>Suspends an active vendor (FR-016): blocks new listings/orders downstream.</summary>
    public void Suspend()
    {
        if (Status == VendorStatus.Suspended) return;
        Status = VendorStatus.Suspended;
        Touch();
    }

    /// <summary>Reinstates a suspended vendor back to Active (FR-016).</summary>
    public void Reinstate()
    {
        if (Status != VendorStatus.Suspended) return;
        Status = VendorStatus.Active;
        Touch();
    }

    /// <summary>Rolls up a delivered order (OrderDelivered subscriber, future wiring).</summary>
    public void RecordFulfilledOrder() { FulfilledOrders++; Touch(); }

    /// <summary>Updates the rolling rating average (ReviewSubmitted subscriber, future wiring).</summary>
    public void UpdateRating(decimal ratingAvg)
    {
        RatingAvg = Math.Clamp(ratingAvg, 0m, 5m);
        Touch();
    }

    private void Touch() => UpdatedUtc = DateTime.UtcNow;
}
