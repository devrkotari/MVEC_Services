using Mvec.BuildingBlocks.Common;
using Mvec.BuildingBlocks.Persistence;
using Mvec.Vendor.Api.Domain;

namespace Mvec.Vendor.Api.Application.Abstractions;

/// <summary>Aggregate-root repository for <see cref="Domain.Vendor"/> (BIGINT key).</summary>
public interface IVendorRepository : IRepository<Domain.Vendor, long>
{
    /// <summary>Loads a (tracked) vendor by its owning user, including KYC documents.</summary>
    Task<Domain.Vendor?> GetByOwnerAsync(long ownerUserId, CancellationToken ct = default);

    /// <summary>Loads a (tracked) vendor including its KYC documents and review log.</summary>
    Task<Domain.Vendor?> GetWithDocumentsAsync(long id, CancellationToken ct = default);

    /// <summary>True if the user already owns a vendor (vnd.Vendors.OwnerUserId is unique).</summary>
    Task<bool> OwnerExistsAsync(long ownerUserId, CancellationToken ct = default);

    /// <summary>Paged admin KYC queue: vendors still Pending or UnderReview (SCR-A03).</summary>
    Task<PagedResult<Domain.Vendor>> ListPendingAsync(PagedRequest request, CancellationToken ct = default);
}

/// <summary>Aggregate-root repository for <see cref="VendorStore"/> (one store per vendor).</summary>
public interface IVendorStoreRepository : IRepository<VendorStore, long>
{
    /// <summary>Loads a (tracked) store for a vendor, including its shipping zones.</summary>
    Task<VendorStore?> GetByVendorAsync(long vendorId, CancellationToken ct = default);

    /// <summary>True if a different store already uses the slug (vnd.VendorStores.Slug is unique).</summary>
    Task<bool> SlugTakenAsync(string slug, long exceptVendorId, CancellationToken ct = default);
}
