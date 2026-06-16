using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Mvec.Identity.Api.Domain;

namespace Mvec.Identity.Api.Infrastructure.Configurations;

// Database-first: entities are MAPPED to the existing idn.* tables and excluded from migrations.
// The schema is owned by the SQL scripts under Database/, not by EF.
internal static class Schemas
{
    public const string Identity = "idn";
}

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users", Schemas.Identity);
        b.HasKey(u => u.Id);
        b.Property(u => u.Id).HasColumnName("UserId").ValueGeneratedOnAdd();

        b.Property(u => u.Email).HasColumnName("Email").IsRequired().HasMaxLength(256);
        b.HasIndex(u => u.Email).IsUnique();

        b.Property(u => u.PasswordHash).HasColumnName("PasswordHash").HasMaxLength(512); // nullable: social-only
        b.Property(u => u.FirstName).HasColumnName("FirstName").IsRequired().HasMaxLength(150);
        b.Property(u => u.LastName).HasColumnName("LastName").IsRequired().HasMaxLength(150);
        b.Property(u => u.PhoneNumber).HasColumnName("PhoneNumber").HasMaxLength(32);
        b.Property(u => u.UserType).HasColumnName("UserType").HasConversion<string>().HasMaxLength(25);
        b.Property(u => u.Status).HasColumnName("Status").HasConversion<string>().HasMaxLength(25);
        b.Property(u => u.EmailConfirmed).HasColumnName("EmailConfirmed");
        b.Property(u => u.PhoneConfirmed).HasColumnName("PhoneConfirmed");
        b.Property(u => u.TwoFactorEnabled).HasColumnName("TwoFactorEnabled");
        b.Property(u => u.TwoFactorSecret).HasColumnName("TwoFactorSecret").HasMaxLength(256);
        b.Property(u => u.AccessFailedCount).HasColumnName("AccessFailedCount");
        b.Property(u => u.LastLoginUtc).HasColumnName("LastLoginUtc");
        b.Property(u => u.CreatedUtc).HasColumnName("CreatedUtc");
        b.Property(u => u.UpdatedUtc).HasColumnName("UpdatedUtc");

        b.HasMany(u => u.RefreshTokens).WithOne().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(u => u.ExternalLogins).WithOne().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(u => u.UserRoles).WithOne().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);

        b.Metadata.FindNavigation(nameof(User.RefreshTokens))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        b.Metadata.FindNavigation(nameof(User.ExternalLogins))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        b.Metadata.FindNavigation(nameof(User.UserRoles))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Roles", Schemas.Identity);
        b.HasKey(r => r.Id);
        b.Property(r => r.Id).HasColumnName("RoleId").ValueGeneratedOnAdd();
        b.Property(r => r.Name).HasColumnName("Name").IsRequired().HasMaxLength(40);
        b.HasIndex(r => r.Name).IsUnique();
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.ToTable("UserRoles", Schemas.Identity);
        b.HasKey(ur => new { ur.UserId, ur.RoleId });
        b.Property(ur => ur.UserId).HasColumnName("UserId");
        b.Property(ur => ur.RoleId).HasColumnName("RoleId");
        b.HasOne<Role>().WithMany().HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> b)
    {
        b.ToTable("ExternalLogins", Schemas.Identity);
        b.HasKey(l => l.Id);
        b.Property(l => l.Id).HasColumnName("ExternalLoginId").ValueGeneratedOnAdd();
        b.Property(l => l.UserId).HasColumnName("UserId");
        b.Property(l => l.Provider).HasColumnName("Provider").IsRequired().HasMaxLength(20);
        b.Property(l => l.ProviderKey).HasColumnName("ProviderKey").IsRequired().HasMaxLength(256);
        b.Property(l => l.CreatedUtc).HasColumnName("CreatedUtc");
        b.HasIndex(l => new { l.Provider, l.ProviderKey }).IsUnique();
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens", Schemas.Identity);
        b.HasKey(t => t.Id);
        b.Property(t => t.Id).HasColumnName("RefreshTokenId").ValueGeneratedOnAdd();
        b.Property(t => t.UserId).HasColumnName("UserId");
        b.Property(t => t.TokenHash).HasColumnName("TokenHash").IsRequired();
        b.Property(t => t.JwtId).HasColumnName("JwtId");
        b.Property(t => t.ExpiresUtc).HasColumnName("ExpiresUtc");
        b.Property(t => t.CreatedUtc).HasColumnName("CreatedUtc");
        b.Property(t => t.RevokedUtc).HasColumnName("RevokedUtc");
        // idn.RefreshTokens.IsRevoked is BIGINT NOT NULL → store the bool flag as 0/1.
        b.Property(t => t.IsRevoked).HasColumnName("IsRevoked").HasConversion(new BoolToZeroOneConverter<long>());
        b.Property(t => t.ReplacedByToken).HasColumnName("ReplacedByToken").HasMaxLength(500);
        b.HasIndex(t => t.UserId);
    }
}

public sealed class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> b)
    {
        b.ToTable("OtpCodes", Schemas.Identity);
        b.HasKey(o => o.Id);
        b.Property(o => o.Id).HasColumnName("OtpId").ValueGeneratedOnAdd();
        b.Property(o => o.UserId).HasColumnName("UserId");
        b.Property(o => o.Purpose).HasColumnName("Purpose").HasConversion<string>().HasMaxLength(25);
        b.Property(o => o.CodeHash).HasColumnName("CodeHash").IsRequired();
        b.Property(o => o.ExpiresUtc).HasColumnName("ExpiresUtc");
        b.Property(o => o.ConsumedUtc).HasColumnName("ConsumedUtc");
        b.Property(o => o.CreatedUtc).HasColumnName("CreatedUtc");
        b.HasIndex(o => new { o.UserId, o.Purpose });
    }
}

public sealed class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> b)
    {
        b.ToTable("UserAddresses", Schemas.Identity);
        b.HasKey(a => a.Id);
        b.Property(a => a.Id).HasColumnName("UserAddressId").ValueGeneratedOnAdd();
        b.Property(a => a.UserId).HasColumnName("UserId");
        b.Property(a => a.Line1).HasColumnName("Line1").IsRequired().HasMaxLength(200);
        b.Property(a => a.Line2).HasColumnName("Line2").HasMaxLength(200);
        b.Property(a => a.City).HasColumnName("City").IsRequired().HasMaxLength(100);
        b.Property(a => a.State).HasColumnName("State").HasMaxLength(100);
        b.Property(a => a.PostalCode).HasColumnName("PostalCode").IsRequired().HasMaxLength(20);
        b.Property(a => a.CountryCode).HasColumnName("CountryCode").IsRequired().HasMaxLength(2);
        b.Property(a => a.Phone).HasColumnName("Phone").HasMaxLength(32);
        b.Property(a => a.IsDefault).HasColumnName("IsDefault");
        b.Property(a => a.CreatedUtc).HasColumnName("CreatedUtc");
        b.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(a => a.UserId);
    }
}
