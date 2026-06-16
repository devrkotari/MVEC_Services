using Mvec.Identity.Api.Domain;

namespace Mvec.Identity.Api.Application.Contracts;

public sealed record RoleDto(int Id, string Name);

public sealed record AssignRoleRequest(int RoleId);

public static class RoleMappings
{
    public static RoleDto ToDto(this Role r) => new(r.Id, r.Name);
}
