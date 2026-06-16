using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mvec.BuildingBlocks.Web;
using Mvec.Identity.Api.Application.Abstractions;
using Mvec.Identity.Api.Application.Contracts;

namespace Mvec.Identity.Api.Api;

/// <summary>The RBAC role catalog (idn.Roles). Admin-only.</summary>
[ApiController]
[Route("api/identity/roles")]
[Authorize(Policy = MvecRoles.Admin)]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleService _roles;

    public RolesController(IRoleService roles) => _roles = roles;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await _roles.ListAsync(ct));
}
