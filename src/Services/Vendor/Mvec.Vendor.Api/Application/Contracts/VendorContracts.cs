using Mvec.Vendor.Api.Domain;

namespace Mvec.Vendor.Api.Application.Contracts;

// ---- Requests ----

public sealed record RegisterVendorRequest(
    string BusinessName,
    string? BusinessType,
    string ContactEmail,
    string? ContactPhone,
    string? Pan,
    string? Gstin);

public sealed record UploadKycRequest(KycDocType DocType, string BlobUrl);

public sealed record RejectVendorRequest(string Reason);

// ---- Responses ----

public sealed record KycDocumentDto(
    long Id,
    KycDocType DocType,
    string BlobUrl,
    KycDocStatus Status,
    DateTime UploadedUtc);

/// <summary>The vendor's own profile + KYC status.</summary>
public sealed record VendorDto(
    long Id,
    long OwnerUserId,
    string BusinessName,
    string? BusinessType,
    string ContactEmail,
    string? ContactPhone,
    string? Pan,
    string? Gstin,
    KycStatus KycStatus,
    VendorTier Tier,
    int ProductLimit,
    decimal CommissionPct,
    VendorStatus Status,
    decimal RatingAvg,
    int FulfilledOrders,
    bool IsApproved,
    DateTime MemberSinceUtc);

/// <summary>Admin detail view: profile + uploaded documents (SCR-A04).</summary>
public sealed record VendorDetailDto(
    VendorDto Vendor,
    IReadOnlyList<KycDocumentDto> Documents);

/// <summary>A single row of the admin KYC queue (SCR-A03).</summary>
public sealed record VendorSummaryDto(
    long Id,
    string BusinessName,
    string ContactEmail,
    KycStatus KycStatus,
    VendorStatus Status,
    DateTime MemberSinceUtc);

public static class VendorMappings
{
    public static KycDocumentDto ToDto(this VendorKycDocument d) =>
        new(d.Id, d.DocType, d.BlobUrl, d.Status, d.UploadedUtc);

    public static VendorDto ToDto(this Domain.Vendor v) => new(
        v.Id, v.OwnerUserId, v.BusinessName, v.BusinessType, v.ContactEmail, v.ContactPhone,
        v.Pan, v.Gstin, v.KycStatus, v.Tier, v.ProductLimit, v.CommissionPct, v.Status,
        v.RatingAvg, v.FulfilledOrders, v.IsApproved, v.MemberSinceUtc);

    public static VendorDetailDto ToDetailDto(this Domain.Vendor v) =>
        new(v.ToDto(), v.KycDocuments.Select(d => d.ToDto()).ToList());

    public static VendorSummaryDto ToSummaryDto(this Domain.Vendor v) =>
        new(v.Id, v.BusinessName, v.ContactEmail, v.KycStatus, v.Status, v.MemberSinceUtc);
}
