using Microsoft.EntityFrameworkCore;
using Mvec.BuildingBlocks.Common;
using Mvec.BuildingBlocks.Persistence;
using Mvec.Identity.Api.Application.Abstractions;
using Mvec.Identity.Api.Domain;

namespace Mvec.Identity.Api.Infrastructure.Repositories;

public sealed class UserRepository(IdentityDbContext db) : EfRepository<User, long>(db), IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, bool includeExternalLogins = false, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        IQueryable<User> query = Set;
        if (includeExternalLogins) query = query.Include(u => u.ExternalLogins);
        return query.FirstOrDefaultAsync(u => u.Email == normalized, ct);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return Set.AnyAsync(u => u.Email == normalized, ct);
    }

    public async Task<PagedResult<User>> ListAsync(PagedRequest request, CancellationToken ct = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        IQueryable<User> query = Set.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.Email.Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(u => u.CreatedUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PagedResult<User>(items, total, page, size);
    }

    public Task<User?> GetWithRefreshTokensAsync(long id, CancellationToken ct = default) =>
        Set.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Id == id, ct);
}

public sealed class RoleRepository(IdentityDbContext db) : EfRepository<Role, int>(db), IRoleRepository
{
    public Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(r => r.Name == name, ct);

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().ToListAsync(ct);
}

public sealed class RefreshTokenRepository(IdentityDbContext db) : EfRepository<RefreshToken, long>(db), IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(byte[] tokenHash, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task<RefreshToken?> GetByHashForUserAsync(byte[] tokenHash, long userId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UserId == userId, ct);
}

public sealed class ExternalLoginRepository(IdentityDbContext db)
    : EfRepository<ExternalLogin, long>(db), IExternalLoginRepository;

public sealed class OtpRepository(IdentityDbContext db) : EfRepository<OtpCode, long>(db), IOtpRepository
{
    public Task<OtpCode?> FindAsync(long userId, OtpPurpose purpose, byte[] codeHash, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(o => o.UserId == userId && o.Purpose == purpose && o.CodeHash == codeHash, ct);
}

public sealed class UserAddressRepository(IdentityDbContext db) : EfRepository<UserAddress, long>(db), IUserAddressRepository
{
    public async Task<IReadOnlyList<UserAddress>> ListByUserAsync(long userId, CancellationToken ct = default) =>
        await Set.Where(a => a.UserId == userId).OrderByDescending(a => a.IsDefault).ThenBy(a => a.Id).ToListAsync(ct);

    public Task<UserAddress?> GetForUserAsync(long addressId, long userId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId, ct);
}

public sealed class UserRoleRepository(IdentityDbContext db) : IUserRoleRepository
{
    private DbSet<UserRole> Set => db.UserRoles;

    public Task<bool> ExistsAsync(long userId, int roleId, CancellationToken ct = default) =>
        Set.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);

    public Task<UserRole?> GetAsync(long userId, int roleId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);

    public async Task<IReadOnlyList<int>> GetRoleIdsAsync(long userId, CancellationToken ct = default) =>
        await Set.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToListAsync(ct);

    public void Add(UserRole userRole) => Set.Add(userRole);
    public void Remove(UserRole userRole) => Set.Remove(userRole);
}
