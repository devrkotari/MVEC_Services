using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mvec.Admin.Api.Application.Abstractions;
using Mvec.Admin.Api.Application.Contracts;
using Mvec.BuildingBlocks.Web;

namespace Mvec.Admin.Api.Api;

/// <summary>
/// Unified admin surface for user management (SCR-A05, FR-017). Proxies the Identity service's
/// admin endpoints, forwarding the admin's JWT. No data is stored here.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = MvecRoles.Admin)]
public sealed class AdminUsersController : ControllerBase
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly IIdentityAdminClient _identity;

    public AdminUsersController(IIdentityAdminClient identity) => _identity = identity;

    /// <summary>Paged list of all users (SCR-A05).</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        (await _identity.ListUsersAsync(Request.QueryString.Value, ct)).Relay();

    /// <summary>Suspend / reinstate a user (FR-017).</summary>
    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> ChangeStatus(long id, ChangeUserStatusRequest request, CancellationToken ct) =>
        (await _identity.ChangeUserStatusAsync(id, JsonSerializer.Serialize(request, Json), ct)).Relay();
}
