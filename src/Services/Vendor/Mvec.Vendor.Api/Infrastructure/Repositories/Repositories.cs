using Microsoft.EntityFrameworkCore;
using Mvec.BuildingBlocks.Common;
using Mvec.BuildingBlocks.Persistence;
using Mvec.Vendor.Api.Application.Abstractions;
using Mvec.Vendor.Api.Domain;

namespace Mvec.Vendor.Api.Infrastructure.Repositories;

public sealed class VendorRepository(VendorDbContext db) : EfRepository<Domain.Vendor, long>(db), IVendorRepository
{
    public Task<Domain.Vendor?> GetByOwnerAsync(long ownerUserId, CancellationToken ct = default) =>
        Set.Include(v => v.KycDocuments).FirstOrDefaultAsync(v => v.OwnerUserId == ownerUserId, ct);

    public Task<Domain.Vendor?> GetWithDocumentsAsync(long id, CancellationToken ct = default) =>
        Set.Include(v => v.KycDocuments).Include(v => v.ReviewLogs).FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<bool> OwnerExistsAsync(long ownerUserId, CancellationToken ct = default) =>
        Set.AnyAsync(v => v.OwnerUserId == ownerUserId, ct);

    public async Task<PagedResult<Domain.Vendor>> ListPendingAsync(PagedRequest request, CancellationToken ct = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        IQueryable<Domain.Vendor> query = Set.AsNoTracking()
            .Where(v => v.KycStatus == KycStatus.Pending || v.KycStatus == KycStatus.UnderReview);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(v => v.BusinessName.ToLower().Contains(term) || v.ContactEmail.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(v => v.MemberSinceUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PagedResult<Domain.Vendor>(items, total, page, size);
    }
}

public sealed class VendorStoreRepository(VendorDbContext db) : EfRepository<VendorStore, long>(db), IVendorStoreRepository
{
    public Task<VendorStore?> GetByVendorAsync(long vendorId, CancellationToken ct = default) =>
        Set.Include(s => s.ShippingZones).FirstOrDefaultAsync(s => s.VendorId == vendorId, ct);

    public Task<bool> SlugTakenAsync(string slug, long exceptVendorId, CancellationToken ct = default) =>
        Set.AnyAsync(s => s.Slug == slug && s.VendorId != exceptVendorId, ct);
}
