using Mvec.BuildingBlocks.Common;
using Mvec.BuildingBlocks.Persistence;
using Mvec.Identity.Api.Application.Abstractions;
using Mvec.Identity.Api.Application.Contracts;
using Mvec.Identity.Api.Domain;

namespace Mvec.Identity.Api.Application.Services;

public sealed class AddressService : IAddressService
{
    private readonly IUserAddressRepository _addresses;
    private readonly IUnitOfWork _uow;

    public AddressService(IUserAddressRepository addresses, IUnitOfWork uow)
    {
        _addresses = addresses;
        _uow = uow;
    }

    public async Task<IReadOnlyList<AddressDto>> ListAsync(long userId, CancellationToken ct = default)
    {
        var items = await _addresses.ListByUserAsync(userId, ct);
        return items.Select(a => a.ToDto()).ToList();
    }

    public async Task<Result<AddressDto>> CreateAsync(long userId, CreateAddressRequest req, CancellationToken ct = default)
    {
        var existing = await _addresses.ListByUserAsync(userId, ct);
        var makeDefault = req.IsDefault || existing.Count == 0;
        if (makeDefault)
            foreach (var a in existing) a.SetDefault(false);

        var address = UserAddress.Create(userId, req.Line1, req.Line2, req.City, req.State,
            req.PostalCode, req.CountryCode.ToUpperInvariant(), req.Phone, makeDefault);
        _addresses.Add(address);

        await _uow.SaveChangesAsync(ct);
        return Result<AddressDto>.Success(address.ToDto());
    }

    public async Task<Result<AddressDto>> UpdateAsync(long userId, long addressId, UpdateAddressRequest req, CancellationToken ct = default)
    {
        var address = await _addresses.GetForUserAsync(addressId, userId, ct);
        if (address is null)
            return Result<AddressDto>.Failure(Error.NotFound("Address not found."));

        address.Update(req.Line1, req.Line2, req.City, req.State,
            req.PostalCode, req.CountryCode.ToUpperInvariant(), req.Phone);

        await _uow.SaveChangesAsync(ct);
        return Result<AddressDto>.Success(address.ToDto());
    }

    public async Task<Result> DeleteAsync(long userId, long addressId, CancellationToken ct = default)
    {
        var address = await _addresses.GetForUserAsync(addressId, userId, ct);
        if (address is null)
            return Result.Failure(Error.NotFound("Address not found."));

        _addresses.Remove(address);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<AddressDto>> SetDefaultAsync(long userId, long addressId, CancellationToken ct = default)
    {
        var all = await _addresses.ListByUserAsync(userId, ct);
        var target = all.FirstOrDefault(a => a.Id == addressId);
        if (target is null)
            return Result<AddressDto>.Failure(Error.NotFound("Address not found."));

        foreach (var a in all) a.SetDefault(a.Id == addressId);

        await _uow.SaveChangesAsync(ct);
        return Result<AddressDto>.Success(target.ToDto());
    }
}
