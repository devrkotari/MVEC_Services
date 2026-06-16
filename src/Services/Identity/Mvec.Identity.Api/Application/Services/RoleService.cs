using Mvec.Identity.Api.Application.Abstractions;
using Mvec.Identity.Api.Application.Contracts;

namespace Mvec.Identity.Api.Application.Services;

public sealed class RoleService : IRoleService
{
    private readonly IRoleRepository _roles;

    public RoleService(IRoleRepository roles) => _roles = roles;

    public async Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken ct = default)
    {
        var roles = await _roles.GetAllAsync(ct);
        return roles.Select(r => r.ToDto()).ToList();
    }
}
