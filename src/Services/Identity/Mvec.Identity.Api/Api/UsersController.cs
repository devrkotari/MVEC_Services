using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mvec.BuildingBlocks.Common;
using Mvec.BuildingBlocks.Web;
using Mvec.Identity.Api.Application.Abstractions;
using Mvec.Identity.Api.Application.Contracts;

namespace Mvec.Identity.Api.Api;

[ApiController]
[Route("api/identity")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ICurrentUser _currentUser;

    public UsersController(IUserService users, ICurrentUser currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    /// <summary>Current authenticated user's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await _users.GetByIdAsync(_currentUser.UserId!.Value, ct);
        return result.ToOk();
    }

    /// <summary>Paged list of all users (FR-017, SCR-A05).</summary>
    [HttpGet("users")]
    [Authorize(Policy = MvecRoles.Admin)]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _users.ListAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Suspend / reinstate a user (FR-017).</summary>
    [HttpPatch("users/{id:long}/status")]
    [Authorize(Policy = MvecRoles.Admin)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeStatus(long id, ChangeStatusRequest request, CancellationToken ct)
    {
        var result = await _users.ChangeStatusAsync(id, request.Status, ct);
        return result.ToOk();
    }

    /// <summary>Change a user's account type (Buyer/Vendor/Admin).</summary>
    [HttpPatch("users/{id:long}/type")]
    [Authorize(Policy = MvecRoles.Admin)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeUserType(long id, ChangeUserTypeRequest request, CancellationToken ct)
    {
        var result = await _users.ChangeUserTypeAsync(id, request.UserType, ct);
        return result.ToOk();
    }

    /// <summary>List a user's assigned RBAC roles (idn.UserRoles).</summary>
    [HttpGet("users/{id:long}/roles")]
    [Authorize(Policy = MvecRoles.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(long id, CancellationToken ct)
    {
        var result = await _users.GetRolesAsync(id, ct);
        return result.ToOk();
    }

    /// <summary>Assign an RBAC role to a user.</summary>
    [HttpPost("users/{id:long}/roles")]
    [Authorize(Policy = MvecRoles.Admin)]
    public async Task<IActionResult> AssignRole(long id, AssignRoleRequest request, CancellationToken ct)
    {
        var result = await _users.AssignRoleAsync(id, request.RoleId, ct);
        return result.ToOk();
    }

    /// <summary>Remove an RBAC role from a user.</summary>
    [HttpDelete("users/{id:long}/roles/{roleId:int}")]
    [Authorize(Policy = MvecRoles.Admin)]
    public async Task<IActionResult> RemoveRole(long id, int roleId, CancellationToken ct)
    {
        var result = await _users.RemoveRoleAsync(id, roleId, ct);
        return result.ToOk();
    }
}
