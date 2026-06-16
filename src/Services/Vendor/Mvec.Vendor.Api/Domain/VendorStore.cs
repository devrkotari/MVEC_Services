namespace Mvec.Vendor.Api.Domain;

/// <summary>
/// A vendor's storefront (vnd.VendorStores) — one per vendor (unique VendorId). Aggregate root that
/// owns its shipping zones. Goes live only once the owning vendor is KYC-approved (SCR-V10 / SCR-B12).
/// </summary>
public class VendorStore
{
    public long Id { get; private set; }
    public long VendorId { get; private set; }
    public string StoreName { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? LogoUrl { get; private set; }
    public string? BannerUrl { get; private set; }
    public string? Description { get; private set; }
    public string? ReturnPolicy { get; private set; }
    public string? SocialLinks { get; private set; }
    public bool IsLive { get; private set; }
    public DateTime CreatedUtc { get; private set; } = DateTime.UtcNow;

    private readonly List<VendorShippingZone> _shippingZones = new();
    public IReadOnlyCollection<VendorShippingZone> ShippingZones => _shippingZones.AsReadOnly();

    private VendorStore() { }

    public static VendorStore Create(long vendorId, string storeName, string slug) => new()
    {
        VendorId = vendorId,
        StoreName = storeName.Trim(),
        Slug = slug.Trim().ToLowerInvariant()
    };

    public void UpdateSettings(string storeName, string? logoUrl, string? bannerUrl,
        string? description, string? returnPolicy, string? socialLinks)
    {
        StoreName = storeName.Trim();
        LogoUrl = logoUrl;
        BannerUrl = bannerUrl;
        Description = description;
        ReturnPolicy = returnPolicy;
        SocialLinks = socialLinks;
    }

    public void SetLive(bool isLive) => IsLive = isLive;

    /// <summary>Replaces the full set of shipping zones (PUT semantics for store settings, SCR-V10).</summary>
    public void ReplaceShippingZones(IEnumerable<(string ZoneName, string? Regions, decimal FlatRate, decimal? FreeAbove)> zones)
    {
        _shippingZones.Clear();
        foreach (var z in zones)
            _shippingZones.Add(VendorShippingZone.Create(Id, z.ZoneName, z.Regions, z.FlatRate, z.FreeAbove));
    }
}
