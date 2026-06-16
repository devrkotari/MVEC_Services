using Mvec.Identity.Api.Domain;

namespace Mvec.Identity.Api.Application.Contracts;

public sealed record UserDto(
    long Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    UserType UserType,
    UserStatus Status,
    bool EmailConfirmed,
    bool PhoneConfirmed,
    bool TwoFactorEnabled,
    DateTime CreatedUtc,
    DateTime? LastLoginUtc);

public sealed record ChangeStatusRequest(UserStatus Status);

public sealed record ChangeUserTypeRequest(UserType UserType);

public static class UserMappings
{
    public static UserDto ToDto(this User u) => new(
        u.Id, u.Email, u.FirstName, u.LastName, u.PhoneNumber, u.UserType, u.Status,
        u.EmailConfirmed, u.PhoneConfirmed, u.TwoFactorEnabled, u.CreatedUtc, u.LastLoginUtc);
}
