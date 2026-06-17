using Microsoft.Extensions.Logging;
using Mvec.Admin.Api.Application.Abstractions;
using Mvec.Admin.Api.Application.Contracts;

namespace Mvec.Admin.Api.Application.Services;

/// <summary>
/// Builds the admin dashboard by fanning out to the downstream services in parallel. A source that
/// fails (service down) or has no provider yet is reported in <c>UnavailableSources</c> rather than
/// failing the whole dashboard.
/// </summary>
public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly IIdentityAdminClient _identity;
    private readonly IVendorAdminClient _vendor;
    private readonly ILogger<AdminDashboardService> _logger;

    public AdminDashboardService(IIdentityAdminClient identity, IVendorAdminClient vendor,
        ILogger<AdminDashboardService> logger)
    {
        _identity = identity;
        _vendor = vendor;
        _logger = logger;
    }

    public async Task<AdminDashboardDto> GetAsync(CancellationToken ct = default)
    {
        var unavailable = new List<string>();

        var usersTask = SafeCountAsync(() => _identity.GetUserCountAsync(ct), "Identity", unavailable);
        var pendingTask = SafeCountAsync(() => _vendor.GetPendingApprovalCountAsync(ct), "Vendor", unavailable);

        var totalUsers = await usersTask;
        var pendingApprovals = await pendingTask;

        // KPIs whose owning service isn't built yet (Order/Analytics) — surfaced as unavailable.
        unavailable.Add("Analytics (GMV)");
        unavailable.Add("Order (disputes)");
        unavailable.Add("Vendor (active count — no endpoint yet)");

        return new AdminDashboardDto(
            TotalUsers: totalUsers,
            PendingVendorApprovals: pendingApprovals,
            ActiveVendors: null,
            GmvToday: null,
            OpenDisputes: null,
            UnavailableSources: unavailable);
    }

    private async Task<int?> SafeCountAsync(Func<Task<int>> call, string source, List<string> unavailable)
    {
        try
        {
            return await call();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dashboard source {Source} unavailable", source);
            unavailable.Add(source);
            return null;
        }
    }
}
