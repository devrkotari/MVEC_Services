using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mvec.Identity.Api.Application.Abstractions;
using Mvec.Identity.Api.Application.Contracts;

namespace Mvec.Identity.Api.Api;

/// <summary>The authenticated user's postal addresses (idn.UserAddresses).</summary>
[ApiController]
[Route("api/identity/me/addresses")]
[Authorize]
public sealed class AddressesController : ControllerBase
{
    private readonly IAddressService _addresses;
    private readonly ICurrentUser _currentUser;

    public AddressesController(IAddressService addresses, ICurrentUser currentUser)
    {
        _addresses = addresses;
        _currentUser = currentUser;
    }

    private long UserId => _currentUser.UserId!.Value;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AddressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await _addresses.ListAsync(UserId, ct));

    [HttpPost]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateAddressRequest request, CancellationToken ct)
    {
        var result = await _addresses.CreateAsync(UserId, request, ct);
        return result.ToCreated("/api/identity/me/addresses");
    }

    [HttpPut("{addressId:long}")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(long addressId, UpdateAddressRequest request, CancellationToken ct)
    {
        var result = await _addresses.UpdateAsync(UserId, addressId, request, ct);
        return result.ToOk();
    }

    [HttpDelete("{addressId:long}")]
    public async Task<IActionResult> Delete(long addressId, CancellationToken ct)
    {
        var result = await _addresses.DeleteAsync(UserId, addressId, ct);
        return result.ToOk();
    }

    [HttpPut("{addressId:long}/default")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetDefault(long addressId, CancellationToken ct)
    {
        var result = await _addresses.SetDefaultAsync(UserId, addressId, ct);
        return result.ToOk();
    }
}
