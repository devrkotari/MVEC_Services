using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mvec.Admin.Api.Application.Abstractions;
using Mvec.Admin.Api.Application.Contracts;
using Mvec.BuildingBlocks.Web;

namespace Mvec.Admin.Api.Api;

/// <summary>Aggregated admin dashboard (SCR-A02), composed from the downstream services.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = MvecRoles.Admin)]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboard;

    public AdminDashboardController(IAdminDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(AdminDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await _dashboard.GetAsync(ct));
}
