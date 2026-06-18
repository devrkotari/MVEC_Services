using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mvec.Admin.Api.Application.Abstractions;
using Mvec.Admin.Api.Application.Contracts;
using Mvec.BuildingBlocks.Web;

namespace Mvec.Admin.Api.Api;

/// <summary>
/// Unified admin surface for vendor management (SCR-A03/A04, FR-016). Proxies the Vendor service's
/// admin endpoints, forwarding the admin's JWT. No data is stored here.
/// </summary>
[ApiController]
[Route("api/admin/vendors")]
[Authorize(Policy = MvecRoles.Admin)]
public sealed class AdminVendorsController : ControllerBase
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly IVendorAdminClient _vendors;

    public AdminVendorsController(IVendorAdminClient vendors) => _vendors = vendors;

    /// <summary>KYC queue: vendors awaiting review (SCR-A03).</summary>
    [HttpGet("pending")]
    public async Task<IActionResult> Pending(CancellationToken ct) =>
        (await _vendors.ListPendingAsync(Request.QueryString.Value, ct)).Relay();

    /// <summary>Directory of approved (live) vendors (SCR-A03).</summary>
    [HttpGet("approved")]
    public async Task<IActionResult> Approved(CancellationToken ct) =>
        (await _vendors.ListApprovedAsync(Request.QueryString.Value, ct)).Relay();

    /// <summary>Vendor detail + KYC documents (SCR-A04).</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detail(long id, CancellationToken ct) =>
        (await _vendors.GetDetailAsync(id, ct)).Relay();

    [HttpPost("{id:long}/approve")]
    public async Task<IActionResult> Approve(long id, CancellationToken ct) =>
        (await _vendors.ApproveAsync(id, ct)).Relay();

    [HttpPost("{id:long}/reject")]
    public async Task<IActionResult> Reject(long id, RejectVendorRequest request, CancellationToken ct) =>
        (await _vendors.RejectAsync(id, JsonSerializer.Serialize(request, Json), ct)).Relay();

    [HttpPost("{id:long}/suspend")]
    public async Task<IActionResult> Suspend(long id, CancellationToken ct) =>
        (await _vendors.SuspendAsync(id, ct)).Relay();

    [HttpPost("{id:long}/reinstate")]
    public async Task<IActionResult> Reinstate(long id, CancellationToken ct) =>
        (await _vendors.ReinstateAsync(id, ct)).Relay();
}
