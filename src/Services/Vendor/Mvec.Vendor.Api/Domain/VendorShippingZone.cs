namespace Mvec.Vendor.Api.Domain;

/// <summary>A shipping zone for a store (vnd.VendorShippingZones). Child of the VendorStore aggregate.</summary>
public class VendorShippingZone
{
    public long Id { get; private set; }
    public long StoreId { get; private set; }
    public string ZoneName { get; private set; } = string.Empty;
    public string? Regions { get; private set; }
    public decimal FlatRate { get; private set; }
    public decimal? FreeAbove { get; private set; }

    private VendorShippingZone() { }

    public static VendorShippingZone Create(long storeId, string zoneName, string? regions,
        decimal flatRate, decimal? freeAbove) => new()
    {
        StoreId = storeId,
        ZoneName = zoneName.Trim(),
        Regions = regions,
        FlatRate = flatRate,
        FreeAbove = freeAbove
    };
}
