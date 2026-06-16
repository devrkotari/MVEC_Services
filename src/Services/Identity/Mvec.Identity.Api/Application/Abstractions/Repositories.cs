using Mvec.BuildingBlocks.Common;
using Mvec.BuildingBlocks.Persistence;
using Mvec.Identity.Api.Domain;

namespace Mvec.Identity.Api.Application.Abstractions;

/// <summary>Aggregate-root repository for <see cref="User"/> (BIGINT key).</summary>
public interface IUserRepository : IRepository<User, long>
{
    /// <summary>Loads a (tracked) user by email, optionally including external logins.</summary>
    Task<User?> GetByEmailAsync(string email, bool includeExternalLogins = false, CancellationToken ct = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    /// <summary>Paged, optionally-searched list of users (read-only).</summary>
    Task<PagedResult<User>> ListAsync(PagedRequest request, CancellationToken ct = default);

    /// <summary>Loads a (tracked) user together with its refresh tokens (needed to revoke on suspend).</summary>
    Task<User?> GetWithRefreshTokensAsync(long id, CancellationToken ct = default);
}

/// <summary>Reference-data repository for <see cref="Role"/> (INT key).</summary>
public interface IRoleRepository : IRepository<Role, int>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default);
}

public interface IRefreshTokenRepository : IRepository<RefreshToken, long>
{
    Task<RefreshToken?> GetByHashAsync(byte[] tokenHash, CancellationToken ct = default);
    Task<RefreshToken?> GetByHashForUserAsync(byte[] tokenHash, long userId, CancellationToken ct = default);
}

public interface IExternalLoginRepository : IRepository<ExternalLogin, long>;

public interface IOtpRepository : IRepository<OtpCode, long>
{
    Task<OtpCode?> FindAsync(long userId, OtpPurpose purpose, byte[] codeHash, CancellationToken ct = default);
}

/// <summary>Postal addresses (idn.UserAddresses), scoped per owning user.</summary>
public interface IUserAddressRepository : IRepository<UserAddress, long>
{
    Task<IReadOnlyList<UserAddress>> ListByUserAsync(long userId, CancellationToken ct = default);
    Task<UserAddress?> GetForUserAsync(long addressId, long userId, CancellationToken ct = default);
}

/// <summary>Join table idn.UserRoles (composite key UserId+RoleId) — assign/remove RBAC roles.</summary>
public interface IUserRoleRepository
{
    Task<bool> ExistsAsync(long userId, int roleId, CancellationToken ct = default);
    Task<UserRole?> GetAsync(long userId, int roleId, CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetRoleIdsAsync(long userId, CancellationToken ct = default);
    void Add(UserRole userRole);
    void Remove(UserRole userRole);
}
