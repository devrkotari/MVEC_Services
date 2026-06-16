using Mvec.BuildingBlocks.Common;
using Mvec.Vendor.Api.Application.Contracts;

namespace Mvec.Vendor.Api.Application.Abstractions;

/// <summary>
/// Vendor onboarding, KYC and the admin lifecycle workflow (WF-002, FR-011/012/016).
/// </summary>
public interface IVendorService
{
    /// <summary>Registers a vendor profile for the caller (PendingKyc) and publishes VendorRegistered.</summary>
    Task<Result<VendorDto>> RegisterAsync(long ownerUserId, RegisterVendorRequest req, CancellationToken ct = default);

    /// <summary>Uploads (or replaces) a KYC document for the caller's vendor (SCR-V02).</summary>
    Task<Result<KycDocumentDto>> UploadKycAsync(long ownerUserId, UploadKycRequest req, CancellationToken ct = default);

    /// <summary>The caller's own vendor profile + KYC status (SCR-V03).</summary>
    Task<Result<VendorDto>> GetMineAsync(long ownerUserId, CancellationToken ct = default);

    /// <summary>Admin KYC queue: vendors awaiting review (SCR-A03).</summary>
    Task<PagedResult<VendorSummaryDto>> ListPendingAsync(PagedRequest request, CancellationToken ct = default);

    /// <summary>Admin detail view: profile + documents (SCR-A04).</summary>
    Task<Result<VendorDetailDto>> GetDetailAsync(long vendorId, CancellationToken ct = default);

    /// <summary>Admin approves KYC → Approved/Active; publishes VendorApproved (FR-012).</summary>
    Task<Result<VendorDto>> ApproveAsync(long vendorId, long adminId, CancellationToken ct = default);

    /// <summary>Admin rejects KYC (reason required); publishes VendorRejected (FR-012).</summary>
    Task<Result<VendorDto>> RejectAsync(long vendorId, long adminId, RejectVendorRequest req, CancellationToken ct = default);

    /// <summary>Admin suspends an active vendor (FR-016).</summary>
    Task<Result<VendorDto>> SuspendAsync(long vendorId, CancellationToken ct = default);

    /// <summary>Admin reinstates a suspended vendor (FR-016).</summary>
    Task<Result<VendorDto>> ReinstateAsync(long vendorId, CancellationToken ct = default);
}
