using Microsoft.EntityFrameworkCore;
using Mvec.BuildingBlocks.Persistence;
using Mvec.Identity.Api.Domain;
using Mvec.Identity.Api.Infrastructure.Configurations;

namespace Mvec.Identity.Api.Infrastructure;

// Database-first: maps to the existing idn.* schema (owned by Database/Schema.sql). EF does not
// create or migrate these tables. The DbContext is the unit-of-work commit boundary.
public class IdentityDbContext : DbContext, IUnitOfWork
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
        modelBuilder.ApplyConfiguration(new ExternalLoginConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new OtpCodeConfiguration());
        modelBuilder.ApplyConfiguration(new UserAddressConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
