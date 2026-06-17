using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvec.Admin.Api.Application.Abstractions;
using Mvec.Admin.Api.Application.Services;
using Xunit;

namespace Mvec.Admin.Tests;

public class AdminDashboardServiceTests
{
    private static AdminDashboardService Build(Mock<IIdentityAdminClient> identity, Mock<IVendorAdminClient> vendor) =>
        new(identity.Object, vendor.Object, NullLogger<AdminDashboardService>.Instance);

    [Fact]
    public async Task Aggregates_counts_from_both_services()
    {
        var identity = new Mock<IIdentityAdminClient>();
        identity.Setup(c => c.GetUserCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(42);
        var vendor = new Mock<IVendorAdminClient>();
        vendor.Setup(c => c.GetPendingApprovalCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(7);

        var result = await Build(identity, vendor).GetAsync();

        result.TotalUsers.Should().Be(42);
        result.PendingVendorApprovals.Should().Be(7);
        result.UnavailableSources.Should().NotContain("Identity").And.NotContain("Vendor");
    }

    [Fact]
    public async Task Failed_source_is_reported_unavailable_without_failing_the_dashboard()
    {
        var identity = new Mock<IIdentityAdminClient>();
        identity.Setup(c => c.GetUserCountAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Identity is down"));
        var vendor = new Mock<IVendorAdminClient>();
        vendor.Setup(c => c.GetPendingApprovalCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var result = await Build(identity, vendor).GetAsync();

        result.TotalUsers.Should().BeNull();
        result.UnavailableSources.Should().Contain("Identity");
        result.PendingVendorApprovals.Should().Be(3); // the healthy source still resolves
    }
}
