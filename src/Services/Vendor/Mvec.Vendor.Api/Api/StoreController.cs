using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mvec.BuildingBlocks.Web;
using Mvec.Vendor.Api.Application.Abstractions;
using Mvec.Vendor.Api.Application.Contracts;

namespace Mvec.Vendor.Api.Api;

/// <summary>Vendor storefront settings (SCR-V10) and the public store page (SCR-B12).</summary>
[ApiController]
[Route("api/vendors")]
public sealed class StoreController : ControllerBase
{
    private readonly IStoreService _stores;
    private readonly ICurrentUser _currentUser;

    public StoreController(IStoreService stores, ICurrentUser currentUser)
    {
        _stores = stores;
        _currentUser = currentUser;
    }

    private long UserId => _currentUser.UserId!.Value;

    /// <summary>Upserts the caller's store settings + shipping zones (SCR-V10).</summary>
    [HttpPut("me/store")]
    [Authorize(Policy = MvecRoles.Vendor)]
    [ProducesResponseType(typeof(StoreDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMyStore(UpdateStoreRequest request, CancellationToken ct)
    {
        var result = await _stores.UpsertMyStoreAsync(UserId, request, ct);
        return result.ToOk();
    }

    /// <summary>Public store page for a vendor (anonymous). Hidden until the vendor is approved (SCR-B12).</summary>
    [HttpGet("{id:long}/store")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicStoreDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicStore(long id, CancellationToken ct)
    {
        var result = await _stores.GetPublicStoreAsync(id, ct);
        return result.ToOk();
    }
}
