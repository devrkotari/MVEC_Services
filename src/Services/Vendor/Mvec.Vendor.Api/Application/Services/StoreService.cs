using System.Text;
using Mvec.BuildingBlocks.Common;
using Mvec.BuildingBlocks.Persistence;
using Mvec.Vendor.Api.Application.Abstractions;
using Mvec.Vendor.Api.Application.Contracts;
using Mvec.Vendor.Api.Domain;

namespace Mvec.Vendor.Api.Application.Services;

public sealed class StoreService : IStoreService
{
    private readonly IVendorStoreRepository _stores;
    private readonly IVendorRepository _vendors;
    private readonly IUnitOfWork _uow;

    public StoreService(IVendorStoreRepository stores, IVendorRepository vendors, IUnitOfWork uow)
    {
        _stores = stores;
        _vendors = vendors;
        _uow = uow;
    }

    public async Task<Result<StoreDto>> UpsertMyStoreAsync(long ownerUserId, UpdateStoreRequest req, CancellationToken ct = default)
    {
        var vendor = await _vendors.GetByOwnerAsync(ownerUserId, ct);
        if (vendor is null)
            return Result<StoreDto>.Failure(Error.NotFound("Vendor profile not found. Register first."));

        var store = await _stores.GetByVendorAsync(vendor.Id, ct);
        if (store is null)
        {
            var slug = await GenerateSlugAsync(req.StoreName, vendor.Id, ct);
            store = VendorStore.Create(vendor.Id, req.StoreName, slug);
            store.UpdateSettings(req.StoreName, req.LogoUrl, req.BannerUrl, req.Description, req.ReturnPolicy, req.SocialLinks);
            _stores.Add(store);
        }
        else
        {
            store.UpdateSettings(req.StoreName, req.LogoUrl, req.BannerUrl, req.Description, req.ReturnPolicy, req.SocialLinks);
        }

        store.ReplaceShippingZones(
            (req.ShippingZones ?? Array.Empty<ShippingZoneDto>())
            .Select(z => (z.ZoneName, z.Regions, z.FlatRate, z.FreeAbove)));

        // A store is only visible publicly once its vendor is KYC-approved (BR-001).
        store.SetLive(vendor.IsApproved);

        await _uow.SaveChangesAsync(ct);
        return Result<StoreDto>.Success(store.ToDto());
    }

    public async Task<Result<PublicStoreDto>> GetPublicStoreAsync(long vendorId, CancellationToken ct = default)
    {
        var vendor = await _vendors.GetByIdAsync(vendorId, ct);
        if (vendor is null || !vendor.IsApproved)
            return Result<PublicStoreDto>.Failure(Error.NotFound("Store not found."));

        var store = await _stores.GetByVendorAsync(vendorId, ct);
        if (store is null || !store.IsLive)
            return Result<PublicStoreDto>.Failure(Error.NotFound("Store not found."));

        return Result<PublicStoreDto>.Success(store.ToPublicDto(vendor));
    }

    /// <summary>Builds a URL-safe, unique slug from the store name (falls back to the vendor id on clash).</summary>
    private async Task<string> GenerateSlugAsync(string storeName, long vendorId, CancellationToken ct)
    {
        var baseSlug = Slugify(storeName);
        if (baseSlug.Length == 0) baseSlug = $"store-{vendorId}";

        var slug = baseSlug;
        if (await _stores.SlugTakenAsync(slug, vendorId, ct))
            slug = $"{baseSlug}-{vendorId}";

        return slug;
    }

    private static string Slugify(string value)
    {
        var sb = new StringBuilder(value.Length);
        var lastDash = false;
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                lastDash = false;
            }
            else if (!lastDash && sb.Length > 0)
            {
                sb.Append('-');
                lastDash = true;
            }
        }
        return sb.ToString().Trim('-');
    }
}
