using Mvec.Identity.Api.Domain;

namespace Mvec.Identity.Api.Application.Contracts;

public sealed record AddressDto(
    long Id,
    string Line1,
    string? Line2,
    string City,
    string? State,
    string PostalCode,
    string CountryCode,
    string? Phone,
    bool IsDefault,
    DateTime CreatedUtc);

public sealed record CreateAddressRequest(
    string Line1,
    string? Line2,
    string City,
    string? State,
    string PostalCode,
    string CountryCode,
    string? Phone,
    bool IsDefault = false);

public sealed record UpdateAddressRequest(
    string Line1,
    string? Line2,
    string City,
    string? State,
    string PostalCode,
    string CountryCode,
    string? Phone);

public static class AddressMappings
{
    public static AddressDto ToDto(this UserAddress a) => new(
        a.Id, a.Line1, a.Line2, a.City, a.State, a.PostalCode, a.CountryCode, a.Phone, a.IsDefault, a.CreatedUtc);
}
