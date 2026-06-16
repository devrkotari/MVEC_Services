using Mvec.BuildingBlocks.Common;
using Mvec.Identity.Api.Application.Contracts;
using Mvec.Identity.Api.Domain;

namespace Mvec.Identity.Api.Application.Abstractions;

/// <summary>Profile read for the current user plus admin user-management (FR-017).</summary>
public interface IUserService
{
    Task<Result<UserDto>> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PagedResult<UserDto>> ListAsync(PagedRequest request, CancellationToken ct = default);
    Task<Result<UserDto>> ChangeStatusAsync(long id, UserStatus status, CancellationToken ct = default);
    Task<Result<UserDto>> ChangeUserTypeAsync(long id, UserType userType, CancellationToken ct = default);

    // ---- RBAC role assignment (idn.UserRoles) ----
    Task<Result<IReadOnlyList<RoleDto>>> GetRolesAsync(long userId, CancellationToken ct = default);
    Task<Result> AssignRoleAsync(long userId, int roleId, CancellationToken ct = default);
    Task<Result> RemoveRoleAsync(long userId, int roleId, CancellationToken ct = default);
}
