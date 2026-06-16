using FluentAssertions;
using Mvec.Identity.Api.Application.Contracts;
using Mvec.Identity.Api.Domain;
using Xunit;

namespace Mvec.Identity.Tests;

public class RoleTests
{
    private static async Task<long> RegisterBuyerAsync(AuthTestHarness h, string email = "buyer@example.com")
    {
        var reg = await h.Auth.RegisterAsync(new RegisterRequest(email, "Passw0rd!", "Test", "Buyer", UserType.Buyer));
        return reg.Value!.Id;
    }

    [Fact]
    public async Task RoleService_lists_seeded_roles()
    {
        using var h = new AuthTestHarness();

        var roles = await h.Roles.ListAsync();

        roles.Select(r => r.Name).Should().BeEquivalentTo("Buyer", "Vendor", "Admin");
    }

    [Fact]
    public async Task Registered_buyer_has_buyer_role()
    {
        using var h = new AuthTestHarness();
        var userId = await RegisterBuyerAsync(h);

        var result = await h.Users.GetRolesAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(r => r.Name).Should().ContainSingle().Which.Should().Be("Buyer");
    }

    [Fact]
    public async Task AssignRole_adds_role_and_is_idempotent()
    {
        using var h = new AuthTestHarness();
        var userId = await RegisterBuyerAsync(h);
        var vendorRole = (await h.Roles.ListAsync()).Single(r => r.Name == "Vendor");

        (await h.Users.AssignRoleAsync(userId, vendorRole.Id)).IsSuccess.Should().BeTrue();
        (await h.Users.AssignRoleAsync(userId, vendorRole.Id)).IsSuccess.Should().BeTrue(); // idempotent

        var roles = (await h.Users.GetRolesAsync(userId)).Value!;
        roles.Select(r => r.Name).Should().BeEquivalentTo("Buyer", "Vendor");
    }

    [Fact]
    public async Task RemoveRole_removes_assignment()
    {
        using var h = new AuthTestHarness();
        var userId = await RegisterBuyerAsync(h);
        var buyerRole = (await h.Roles.ListAsync()).Single(r => r.Name == "Buyer");

        var removed = await h.Users.RemoveRoleAsync(userId, buyerRole.Id);

        removed.IsSuccess.Should().BeTrue();
        (await h.Users.GetRolesAsync(userId)).Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task AssignRole_unknown_role_returns_not_found()
    {
        using var h = new AuthTestHarness();
        var userId = await RegisterBuyerAsync(h);

        var result = await h.Users.AssignRoleAsync(userId, 9999);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("not_found");
    }
}
