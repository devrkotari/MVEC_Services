using FluentAssertions;
using Mvec.Vendor.Api.Application.Contracts;
using Xunit;

namespace Mvec.Vendor.Tests;

public class StoreServiceTests
{
    private const long OwnerId = 2002;
    private const long AdminId = 9001;

    private static RegisterVendorRequest NewVendor() =>
        new("Bright Bazaar", "Company", "bright@example.com", null, null, null);

    private static UpdateStoreRequest NewStore() => new(
        "Bright Bazaar Store", "https://cdn/logo.png", "https://cdn/banner.png",
        "Quality goods.", "30-day returns.", "{\"x\":\"@bright\"}",
        new[] { new ShippingZoneDto("Metro", "Hyderabad,Pune", 49.00m, 999.00m) });

    private static async Task<long> RegisterAndApproveAsync(VendorTestHarness h)
    {
        var reg = await h.Vendors.RegisterAsync(OwnerId, NewVendor());
        await h.Vendors.ApproveAsync(reg.Value!.Id, AdminId);
        return reg.Value.Id;
    }

    [Fact]
    public async Task Upsert_store_creates_slug_and_shipping_zones()
    {
        using var h = new VendorTestHarness();
        await RegisterAndApproveAsync(h);

        var result = await h.Stores.UpsertMyStoreAsync(OwnerId, NewStore());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Slug.Should().Be("bright-bazaar-store");
        result.Value.ShippingZones.Should().ContainSingle().Which.FlatRate.Should().Be(49.00m);
        result.Value.IsLive.Should().BeTrue(); // approved vendor → live
    }

    [Fact]
    public async Task Upsert_replaces_shipping_zones()
    {
        using var h = new VendorTestHarness();
        await RegisterAndApproveAsync(h);
        await h.Stores.UpsertMyStoreAsync(OwnerId, NewStore());

        var replaced = NewStore() with
        {
            ShippingZones = new[]
            {
                new ShippingZoneDto("National", null, 79.00m, null),
                new ShippingZoneDto("Express", null, 149.00m, null)
            }
        };
        var result = await h.Stores.UpsertMyStoreAsync(OwnerId, replaced);

        result.Value!.ShippingZones.Should().HaveCount(2);
        result.Value.ShippingZones.Select(z => z.ZoneName).Should().BeEquivalentTo("National", "Express");
    }

    [Fact]
    public async Task Public_store_returns_profile_and_rating_for_approved_vendor()
    {
        using var h = new VendorTestHarness();
        var vendorId = await RegisterAndApproveAsync(h);
        await h.Stores.UpsertMyStoreAsync(OwnerId, NewStore());

        var result = await h.Stores.GetPublicStoreAsync(vendorId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BusinessName.Should().Be("Bright Bazaar");
        result.Value.StoreName.Should().Be("Bright Bazaar Store");
        result.Value.ShippingZones.Should().ContainSingle();
        result.Value.RatingAvg.Should().Be(0m);
        result.Value.FulfilledOrders.Should().Be(0);
    }

    [Fact]
    public async Task Public_store_is_hidden_for_unapproved_vendor()
    {
        using var h = new VendorTestHarness();
        var reg = await h.Vendors.RegisterAsync(OwnerId, NewVendor()); // not approved
        await h.Stores.UpsertMyStoreAsync(OwnerId, NewStore());

        var result = await h.Stores.GetPublicStoreAsync(reg.Value!.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task Upsert_store_before_registering_is_not_found()
    {
        using var h = new VendorTestHarness();

        var result = await h.Stores.UpsertMyStoreAsync(OwnerId, NewStore());

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("not_found");
    }
}
