using Mvec.BuildingBlocks.Common;
using Mvec.Identity.Api.Application.Contracts;

namespace Mvec.Identity.Api.Application.Abstractions;

/// <summary>Manages the current user's postal addresses (idn.UserAddresses).</summary>
public interface IAddressService
{
    Task<IReadOnlyList<AddressDto>> ListAsync(long userId, CancellationToken ct = default);
    Task<Result<AddressDto>> CreateAsync(long userId, CreateAddressRequest req, CancellationToken ct = default);
    Task<Result<AddressDto>> UpdateAsync(long userId, long addressId, UpdateAddressRequest req, CancellationToken ct = default);
    Task<Result> DeleteAsync(long userId, long addressId, CancellationToken ct = default);
    Task<Result<AddressDto>> SetDefaultAsync(long userId, long addressId, CancellationToken ct = default);
}
