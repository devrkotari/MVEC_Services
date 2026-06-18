using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mvec.BuildingBlocks.Common;
using Mvec.BuildingBlocks.Web;
using Mvec.Vendor.Api.Application.Abstractions;
using Mvec.Vendor.Api.Application.Contracts;

namespace Mvec.Vendor.Api.Api;

/// <summary>Vendor onboarding, KYC and the admin lifecycle workflow (WF-002).</summary>
[ApiController]
[Route("api/vendors")]
public sealed class VendorController : ControllerBase
{
    private readonly IVendorService _vendors;
    private readonly ICurrentUser _currentUser;

    public VendorController(IVendorService vendors, ICurrentUser currentUser)
    {
        _vendors = vendors;
        _currentUser = currentUser;
    }

    private long UserId => _currentUser.UserId!.Value;

    // ---- Vendor self-service ----

    /// <summary>Registers the caller as a vendor (→ PendingKyc, publishes VendorRegistered).</summary>
    [HttpPost("register")]
    [Authorize(Policy = MvecRoles.Vendor)]
    [ProducesResponseType(typeof(VendorDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterVendorRequest request, CancellationToken ct)
    {
        var result = await _vendors.RegisterAsync(UserId, request, ct);
        return result.ToCreated("/api/vendors/me");
    }

    /// <summary>Uploads (or replaces) a KYC document for the caller's vendor (SCR-V02).</summary>
    [HttpPost("me/kyc")]
    [Authorize(Policy = MvecRoles.Vendor)]
    [ProducesResponseType(typeof(KycDocumentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadKyc(UploadKycRequest request, CancellationToken ct)
    {
        var result = await _vendors.UploadKycAsync(UserId, request, ct);
        return result.ToOk();
    }

    /// <summary>The caller's own vendor profile + KYC status (SCR-V03).</summary>
    [HttpGet("me")]
    [Authorize(Policy = MvecRoles.Vendor)]
    [ProducesResponseType(typeof(VendorDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await _vendors.GetMineAsync(UserId, ct);
        return result.ToOk();
    }

    // ---- Admin review & lifecycle ----

    /// <summary>Admin KYC queue: vendors awaiting review (SCR-A03).</summary>
    [HttpGet("pending")]
    [Authorize(Policy = MvecRoles.Admin)]
    [ProducesResponseType(typeof(PagedResult<VendorSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPending([FromQuery] PagedRequest request, CancellationToken ct) =>
        Ok(await _vendors.ListPendingAsync(request, ct));

    /// <summary>Admin directory of approved (live) vendors (SCR-A03).</summary>
    [HttpGet("approved")]
    [Authorize(Policy = MvecRoles.Admin)]
    [ProducesResponseType(typeof(PagedResult<VendorSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListApproved([FromQuery] PagedRequest request, CancellationToken ct) =>
        Ok(await _vendors.ListApprovedAsync(request, ct));

    /// <summary>Admin detail view: profile + uploaded documents (SCR-A04).</summary>
    [HttpGet("{id:long}")]
    [Authorize(Policy = MvecRoles.Admin)]
    [ProducesResponseType(typeof(VendorDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetail(long id, CancellationToken ct)
    {
        var result = await _vendors.GetDetailAsync(id, ct);
        return result.ToOk();
    }

    /// <summary>Admin approves KYC → Approved/Active, publishes VendorApproved (FR-012).</summary>
    [HttpPost("{id:long}/approve")]
    [Authorize(Policy = MvecRoles.Admin)]
    [ProducesResponseType(typeof(VendorDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
    {
        var result = await _vendors.ApproveAsync(id, UserId, ct);
        return result.ToOk();
    }

    /// <summary>Admin rejects KYC (reason required), publishes VendorRejected (FR-012).</summary>
    [HttpPost("{id:long}/reject")]
    [Authorize(Policy = MvecRoles.Admin)]
    [ProducesResponseType(typeof(VendorDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(long id, RejectVendorRequest request, CancellationToken ct)
    {
        var result = await _vendors.RejectAsync(id, UserId, request, ct);
        return result.ToOk();
    }

    /// <summary>Admin suspends an active vendor (FR-016).</summary>
    [HttpPost("{id:long}/suspend")]
    [Authorize(Policy = MvecRoles.Admin)]
    [ProducesResponseType(typeof(VendorDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Suspend(long id, CancellationToken ct)
    {
        var result = await _vendors.SuspendAsync(id, ct);
        return result.ToOk();
    }

    /// <summary>Admin reinstates a suspended vendor (FR-016).</summary>
    [HttpPost("{id:long}/reinstate")]
    [Authorize(Policy = MvecRoles.Admin)]
    [ProducesResponseType(typeof(VendorDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reinstate(long id, CancellationToken ct)
    {
        var result = await _vendors.ReinstateAsync(id, ct);
        return result.ToOk();
    }
}
