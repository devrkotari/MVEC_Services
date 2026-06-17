using Mvec.Admin.Api.Application.Contracts;

namespace Mvec.Admin.Api.Application.Abstractions;

/// <summary>Aggregates the admin dashboard (SCR-A02) by fanning out to the downstream services.</summary>
public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetAsync(CancellationToken ct = default);
}
