using FluentAssertions;
using Mvec.Identity.Api.Application.Contracts;
using Mvec.Identity.Api.Domain;
using Xunit;

namespace Mvec.Identity.Tests;

public class AddressServiceTests
{
    private static async Task<long> RegisterBuyerAsync(AuthTestHarness h, string email = "buyer@example.com")
    {
        var reg = await h.Auth.RegisterAsync(new RegisterRequest(email, "Passw0rd!", "Test", "Buyer", UserType.Buyer));
        return reg.Value!.Id;
    }

    private static CreateAddressRequest NewAddress(bool isDefault = false) =>
        new("12 Main St", null, "Hyderabad", "TS", "500001", "in", "+910000000000", isDefault);

    [Fact]
    public async Task Create_first_address_is_default_even_when_not_requested()
    {
        using var h = new AuthTestHarness();
        var userId = await RegisterBuyerAsync(h);

        var result = await h.Addresses.CreateAsync(userId, NewAddress(isDefault: false));

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDefault.Should().BeTrue();
        result.Value.CountryCode.Should().Be("IN"); // normalized to upper-case
    }

    [Fact]
    public async Task Create_default_address_unsets_previous_default()
    {
        using var h = new AuthTestHarness();
        var userId = await RegisterBuyerAsync(h);
        var first = await h.Addresses.CreateAsync(userId, NewAddress(isDefault: true));

        var second = await h.Addresses.CreateAsync(userId, NewAddress(isDefault: true));

        second.Value!.IsDefault.Should().BeTrue();
        var list = await h.Addresses.ListAsync(userId);
        list.Single(a => a.Id == first.Value!.Id).IsDefault.Should().BeFalse();
        list.Count(a => a.IsDefault).Should().Be(1);
    }

    [Fact]
    public async Task Update_changes_fields()
    {
        using var h = new AuthTestHarness();
        var userId = await RegisterBuyerAsync(h);
        var created = await h.Addresses.CreateAsync(userId, NewAddress());

        var updated = await h.Addresses.UpdateAsync(userId, created.Value!.Id,
            new UpdateAddressRequest("99 New Rd", "Apt 4", "Pune", "MH", "411001", "in", null));

        updated.IsSuccess.Should().BeTrue();
        updated.Value!.Line1.Should().Be("99 New Rd");
        updated.Value.City.Should().Be("Pune");
    }

    [Fact]
    public async Task Update_other_users_address_returns_not_found()
    {
        using var h = new AuthTestHarness();
        var owner = await RegisterBuyerAsync(h, "owner@example.com");
        var other = await RegisterBuyerAsync(h, "other@example.com");
        var created = await h.Addresses.CreateAsync(owner, NewAddress());

        var result = await h.Addresses.UpdateAsync(other, created.Value!.Id,
            new UpdateAddressRequest("x", null, "y", null, "1", "in", null));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task SetDefault_switches_default_address()
    {
        using var h = new AuthTestHarness();
        var userId = await RegisterBuyerAsync(h);
        var first = await h.Addresses.CreateAsync(userId, NewAddress());
        var second = await h.Addresses.CreateAsync(userId, NewAddress());

        var result = await h.Addresses.SetDefaultAsync(userId, second.Value!.Id);

        result.IsSuccess.Should().BeTrue();
        var list = await h.Addresses.ListAsync(userId);
        list.Single(a => a.Id == second.Value!.Id).IsDefault.Should().BeTrue();
        list.Single(a => a.Id == first.Value!.Id).IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_removes_address()
    {
        using var h = new AuthTestHarness();
        var userId = await RegisterBuyerAsync(h);
        var created = await h.Addresses.CreateAsync(userId, NewAddress());

        var result = await h.Addresses.DeleteAsync(userId, created.Value!.Id);

        result.IsSuccess.Should().BeTrue();
        (await h.Addresses.ListAsync(userId)).Should().BeEmpty();
    }
}
