using Mvec.Vendor.Api.Domain;

namespace Mvec.Vendor.Api.Application.Contracts;

public sealed record ShippingZoneDto(
    string ZoneName,
    string? Regions,
    decimal FlatRate,
    decimal? FreeAbove);

/// <summary>Store settings upsert (SCR-V10). Replaces the full shipping-zone set.</summary>
public sealed record UpdateStoreRequest(
    string StoreName,
    string? LogoUrl,
    string? BannerUrl,
    string? Description,
    string? ReturnPolicy,
    string? SocialLinks,
    IReadOnlyList<ShippingZoneDto>? ShippingZones);

public sealed record StoreDto(
    long Id,
    long VendorId,
    string StoreName,
    string Slug,
    string? LogoUrl,
    string? BannerUrl,
    string? Description,
    string? ReturnPolicy,
    string? SocialLinks,
    bool IsLive,
    IReadOnlyList<ShippingZoneDto> ShippingZones);

/// <summary>Public store page (SCR-B12): storefront + vendor rating/order-count surface.</summary>
public sealed record PublicStoreDto(
    long VendorId,
    string BusinessName,
    string StoreName,
    string Slug,
    string? LogoUrl,
    string? BannerUrl,
    string? Description,
    string? ReturnPolicy,
    string? SocialLinks,
    decimal RatingAvg,
    int FulfilledOrders,
    IReadOnlyList<ShippingZoneDto> ShippingZones);

public static class StoreMappings
{
    public static ShippingZoneDto ToDto(this VendorShippingZone z) =>
        new(z.ZoneName, z.Regions, z.FlatRate, z.FreeAbove);

    public static StoreDto ToDto(this VendorStore s) => new(
        s.Id, s.VendorId, s.StoreName, s.Slug, s.LogoUrl, s.BannerUrl, s.Description,
        s.ReturnPolicy, s.SocialLinks, s.IsLive, s.ShippingZones.Select(z => z.ToDto()).ToList());

    public static PublicStoreDto ToPublicDto(this VendorStore s, Domain.Vendor v) => new(
        v.Id, v.BusinessName, s.StoreName, s.Slug, s.LogoUrl, s.BannerUrl, s.Description,
        s.ReturnPolicy, s.SocialLinks, v.RatingAvg, v.FulfilledOrders,
        s.ShippingZones.Select(z => z.ToDto()).ToList());
}
