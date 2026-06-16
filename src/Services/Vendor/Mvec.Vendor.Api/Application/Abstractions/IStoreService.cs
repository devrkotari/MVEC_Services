using Mvec.BuildingBlocks.Common;
using Mvec.Vendor.Api.Application.Contracts;

namespace Mvec.Vendor.Api.Application.Abstractions;

/// <summary>Vendor storefront settings (SCR-V10) and the public store page (SCR-B12).</summary>
public interface IStoreService
{
    /// <summary>Upserts the caller's store settings + shipping zones (SCR-V10).</summary>
    Task<Result<StoreDto>> UpsertMyStoreAsync(long ownerUserId, UpdateStoreRequest req, CancellationToken ct = default);

    /// <summary>Public store page by vendor id. Hidden (not-found) unless the vendor is approved (SCR-B12).</summary>
    Task<Result<PublicStoreDto>> GetPublicStoreAsync(long vendorId, CancellationToken ct = default);
}
