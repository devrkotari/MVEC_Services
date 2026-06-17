namespace Mvec.Admin.Api.Application.Contracts;

/// <summary>
/// Aggregated admin dashboard KPIs (SCR-A02), assembled from multiple services. Nullable metrics
/// whose source service isn't live yet are returned as null and named in <see cref="UnavailableSources"/>.
/// </summary>
public sealed record AdminDashboardDto(
    int? TotalUsers,
    int? PendingVendorApprovals,
    int? ActiveVendors,
    decimal? GmvToday,
    int? OpenDisputes,
    IReadOnlyList<string> UnavailableSources);
