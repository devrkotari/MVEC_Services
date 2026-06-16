using Mvec.Identity.Api.Application.Contracts;

namespace Mvec.Identity.Api.Application.Abstractions;

/// <summary>Reads the RBAC role catalog (idn.Roles).</summary>
public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken ct = default);
}
