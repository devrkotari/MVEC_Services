using Mvec.BuildingBlocks.Common;
using Mvec.BuildingBlocks.Persistence;
using Mvec.Identity.Api.Application.Abstractions;
using Mvec.Identity.Api.Application.Contracts;
using Mvec.Identity.Api.Domain;

namespace Mvec.Identity.Api.Application.Services;

/// <summary>Profile read for the current user plus admin user-management (FR-017) and role assignment.</summary>
public sealed class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IUserRoleRepository _userRoles;
    private readonly IUnitOfWork _uow;

    public UserService(IUserRepository users, IRoleRepository roles, IUserRoleRepository userRoles, IUnitOfWork uow)
    {
        _users = users;
        _roles = roles;
        _userRoles = userRoles;
        _uow = uow;
    }

    public async Task<Result<UserDto>> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct);
        return user is null
            ? Result<UserDto>.Failure(Error.NotFound("User not found."))
            : Result<UserDto>.Success(user.ToDto());
    }

    public async Task<PagedResult<UserDto>> ListAsync(PagedRequest request, CancellationToken ct = default)
    {
        var page = await _users.ListAsync(request, ct);
        var items = page.Items.Select(u => u.ToDto()).ToList();
        return new PagedResult<UserDto>(items, page.Total, page.Page, page.PageSize);
    }

    public async Task<Result<UserDto>> ChangeStatusAsync(long id, UserStatus status, CancellationToken ct = default)
    {
        var user = await _users.GetWithRefreshTokensAsync(id, ct);
        if (user is null)
            return Result<UserDto>.Failure(Error.NotFound("User not found."));

        if (status == UserStatus.Suspended) user.Suspend();
        else if (status == UserStatus.Active) user.Reinstate();

        await _uow.SaveChangesAsync(ct);
        return Result<UserDto>.Success(user.ToDto());
    }

    public async Task<Result<UserDto>> ChangeUserTypeAsync(long id, UserType userType, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null)
            return Result<UserDto>.Failure(Error.NotFound("User not found."));

        user.ChangeUserType(userType);
        await _uow.SaveChangesAsync(ct);
        return Result<UserDto>.Success(user.ToDto());
    }

    public async Task<Result<IReadOnlyList<RoleDto>>> GetRolesAsync(long userId, CancellationToken ct = default)
    {
        if (await _users.GetByIdAsync(userId, ct) is null)
            return Result<IReadOnlyList<RoleDto>>.Failure(Error.NotFound("User not found."));

        var roleIds = await _userRoles.GetRoleIdsAsync(userId, ct);
        var roles = (await _roles.GetAllAsync(ct))
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.ToDto())
            .ToList();
        return Result<IReadOnlyList<RoleDto>>.Success(roles);
    }

    public async Task<Result> AssignRoleAsync(long userId, int roleId, CancellationToken ct = default)
    {
        if (await _users.GetByIdAsync(userId, ct) is null)
            return Result.Failure(Error.NotFound("User not found."));
        if (await _roles.GetByIdAsync(roleId, ct) is null)
            return Result.Failure(Error.NotFound("Role not found."));

        if (await _userRoles.ExistsAsync(userId, roleId, ct))
            return Result.Success(); // already assigned — idempotent

        _userRoles.Add(UserRole.Create(userId, roleId));
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveRoleAsync(long userId, int roleId, CancellationToken ct = default)
    {
        var link = await _userRoles.GetAsync(userId, roleId, ct);
        if (link is null)
            return Result.Failure(Error.NotFound("Role assignment not found."));

        _userRoles.Remove(link);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
