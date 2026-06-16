using Mvec.BuildingBlocks.Common;
using Mvec.BuildingBlocks.Persistence;
using Mvec.Contracts.Events;
using Mvec.Vendor.Api.Application.Abstractions;
using Mvec.Vendor.Api.Application.Contracts;
using Mvec.Vendor.Api.Domain;

namespace Mvec.Vendor.Api.Application.Services;

public sealed class VendorService : IVendorService
{
    private readonly IVendorRepository _vendors;
    private readonly IUnitOfWork _uow;
    private readonly IEventPublisher _events;

    public VendorService(IVendorRepository vendors, IUnitOfWork uow, IEventPublisher events)
    {
        _vendors = vendors;
        _uow = uow;
        _events = events;
    }

    public async Task<Result<VendorDto>> RegisterAsync(long ownerUserId, RegisterVendorRequest req, CancellationToken ct = default)
    {
        if (await _vendors.OwnerExistsAsync(ownerUserId, ct))
            return Result<VendorDto>.Failure(Error.Conflict("This account already has a vendor profile."));

        var vendor = Domain.Vendor.Register(ownerUserId, req.BusinessName, req.BusinessType,
            req.ContactEmail, req.ContactPhone, req.Pan, req.Gstin);
        _vendors.Add(vendor);
        await _uow.SaveChangesAsync(ct);

        // Post-commit publish: the write is already durable, so it must NOT observe the request's
        // cancellation token — a client/gateway abort here would otherwise throw after the fact.
        await _events.PublishAsync(
            new VendorRegistered(vendor.Id, vendor.OwnerUserId, vendor.BusinessName, DateTime.UtcNow));

        return Result<VendorDto>.Success(vendor.ToDto());
    }

    public async Task<Result<KycDocumentDto>> UploadKycAsync(long ownerUserId, UploadKycRequest req, CancellationToken ct = default)
    {
        var vendor = await _vendors.GetByOwnerAsync(ownerUserId, ct);
        if (vendor is null)
            return Result<KycDocumentDto>.Failure(Error.NotFound("Vendor profile not found. Register first."));

        var doc = vendor.SubmitKycDocument(req.DocType, req.BlobUrl);
        await _uow.SaveChangesAsync(ct);

        return Result<KycDocumentDto>.Success(doc.ToDto());
    }

    public async Task<Result<VendorDto>> GetMineAsync(long ownerUserId, CancellationToken ct = default)
    {
        var vendor = await _vendors.GetByOwnerAsync(ownerUserId, ct);
        return vendor is null
            ? Result<VendorDto>.Failure(Error.NotFound("Vendor profile not found."))
            : Result<VendorDto>.Success(vendor.ToDto());
    }

    public async Task<PagedResult<VendorSummaryDto>> ListPendingAsync(PagedRequest request, CancellationToken ct = default)
    {
        var page = await _vendors.ListPendingAsync(request, ct);
        var items = page.Items.Select(v => v.ToSummaryDto()).ToList();
        return new PagedResult<VendorSummaryDto>(items, page.Total, page.Page, page.PageSize);
    }

    public async Task<Result<VendorDetailDto>> GetDetailAsync(long vendorId, CancellationToken ct = default)
    {
        var vendor = await _vendors.GetWithDocumentsAsync(vendorId, ct);
        return vendor is null
            ? Result<VendorDetailDto>.Failure(Error.NotFound("Vendor not found."))
            : Result<VendorDetailDto>.Success(vendor.ToDetailDto());
    }

    public async Task<Result<VendorDto>> ApproveAsync(long vendorId, long adminId, CancellationToken ct = default)
    {
        var vendor = await _vendors.GetWithDocumentsAsync(vendorId, ct);
        if (vendor is null)
            return Result<VendorDto>.Failure(Error.NotFound("Vendor not found."));
        if (vendor.Status == VendorStatus.Closed)
            return Result<VendorDto>.Failure(Error.Conflict("Cannot approve a closed vendor."));

        vendor.Approve(adminId);
        await _uow.SaveChangesAsync(ct);

        await _events.PublishAsync(
            new VendorApproved(vendor.Id, vendor.OwnerUserId, DateTime.UtcNow));

        return Result<VendorDto>.Success(vendor.ToDto());
    }

    public async Task<Result<VendorDto>> RejectAsync(long vendorId, long adminId, RejectVendorRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Reason))
            return Result<VendorDto>.Failure(Error.Validation("A rejection reason is required."));

        var vendor = await _vendors.GetWithDocumentsAsync(vendorId, ct);
        if (vendor is null)
            return Result<VendorDto>.Failure(Error.NotFound("Vendor not found."));
        if (vendor.Status == VendorStatus.Closed)
            return Result<VendorDto>.Failure(Error.Conflict("Cannot reject a closed vendor."));

        var reason = req.Reason.Trim();
        vendor.Reject(adminId, reason);
        await _uow.SaveChangesAsync(ct);

        await _events.PublishAsync(
            new VendorRejected(vendor.Id, vendor.OwnerUserId, reason, DateTime.UtcNow));

        return Result<VendorDto>.Success(vendor.ToDto());
    }

    public async Task<Result<VendorDto>> SuspendAsync(long vendorId, CancellationToken ct = default)
    {
        var vendor = await _vendors.GetByIdAsync(vendorId, ct);
        if (vendor is null)
            return Result<VendorDto>.Failure(Error.NotFound("Vendor not found."));
        if (vendor.Status != VendorStatus.Active)
            return Result<VendorDto>.Failure(Error.Conflict("Only an active vendor can be suspended."));

        vendor.Suspend();
        await _uow.SaveChangesAsync(ct);
        return Result<VendorDto>.Success(vendor.ToDto());
    }

    public async Task<Result<VendorDto>> ReinstateAsync(long vendorId, CancellationToken ct = default)
    {
        var vendor = await _vendors.GetByIdAsync(vendorId, ct);
        if (vendor is null)
            return Result<VendorDto>.Failure(Error.NotFound("Vendor not found."));
        if (vendor.Status != VendorStatus.Suspended)
            return Result<VendorDto>.Failure(Error.Conflict("Only a suspended vendor can be reinstated."));

        vendor.Reinstate();
        await _uow.SaveChangesAsync(ct);
        return Result<VendorDto>.Success(vendor.ToDto());
    }
}
