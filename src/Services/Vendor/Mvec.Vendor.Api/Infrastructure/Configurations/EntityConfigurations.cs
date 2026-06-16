using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mvec.Vendor.Api.Domain;

namespace Mvec.Vendor.Api.Infrastructure.Configurations;

// Database-first: entities are MAPPED to the existing vnd.* tables (owned by Database/Schema.sql).
// EF does not create or migrate them. Enums persist as their VARCHAR string names.
internal static class Schemas
{
    public const string Vendor = "vnd";
}

public sealed class VendorConfiguration : IEntityTypeConfiguration<Domain.Vendor>
{
    public void Configure(EntityTypeBuilder<Domain.Vendor> b)
    {
        b.ToTable("Vendors", Schemas.Vendor);
        b.HasKey(v => v.Id);
        b.Property(v => v.Id).HasColumnName("VendorId").ValueGeneratedOnAdd();

        b.Property(v => v.OwnerUserId).HasColumnName("OwnerUserId");
        b.HasIndex(v => v.OwnerUserId).IsUnique();

        b.Property(v => v.BusinessName).HasColumnName("BusinessName").IsRequired().HasMaxLength(200);
        b.Property(v => v.BusinessType).HasColumnName("BusinessType").HasMaxLength(50);
        b.Property(v => v.ContactEmail).HasColumnName("ContactEmail").IsRequired().HasMaxLength(256);
        b.Property(v => v.ContactPhone).HasColumnName("ContactPhone").HasMaxLength(32);
        b.Property(v => v.Pan).HasColumnName("PAN").HasColumnType("varchar(15)");
        b.Property(v => v.Gstin).HasColumnName("GSTIN").HasColumnType("varchar(20)");
        b.Property(v => v.KycStatus).HasColumnName("KycStatus").HasConversion<string>().HasColumnType("varchar(15)");
        b.Property(v => v.Tier).HasColumnName("Tier").HasConversion<string>().HasColumnType("varchar(10)");
        b.Property(v => v.ProductLimit).HasColumnName("ProductLimit");
        b.Property(v => v.CommissionPct).HasColumnName("CommissionPct").HasColumnType("decimal(5,2)");
        b.Property(v => v.Status).HasColumnName("Status").HasConversion<string>().HasColumnType("varchar(15)");
        b.Property(v => v.RatingAvg).HasColumnName("RatingAvg").HasColumnType("decimal(3,2)");
        b.Property(v => v.FulfilledOrders).HasColumnName("FulfilledOrders");
        b.Property(v => v.MemberSinceUtc).HasColumnName("MemberSinceUtc");
        b.Property(v => v.UpdatedUtc).HasColumnName("UpdatedUtc");

        b.HasMany(v => v.KycDocuments).WithOne().HasForeignKey(d => d.VendorId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.ReviewLogs).WithOne().HasForeignKey(l => l.VendorId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.BankAccounts).WithOne().HasForeignKey(a => a.VendorId).OnDelete(DeleteBehavior.Cascade);

        b.Metadata.FindNavigation(nameof(Domain.Vendor.KycDocuments))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        b.Metadata.FindNavigation(nameof(Domain.Vendor.ReviewLogs))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        b.Metadata.FindNavigation(nameof(Domain.Vendor.BankAccounts))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class VendorKycDocumentConfiguration : IEntityTypeConfiguration<VendorKycDocument>
{
    public void Configure(EntityTypeBuilder<VendorKycDocument> b)
    {
        b.ToTable("VendorKycDocuments", Schemas.Vendor);
        b.HasKey(d => d.Id);
        b.Property(d => d.Id).HasColumnName("DocumentId").ValueGeneratedOnAdd();
        b.Property(d => d.VendorId).HasColumnName("VendorId");
        b.Property(d => d.DocType).HasColumnName("DocType").HasConversion<string>().HasColumnType("varchar(20)");
        b.Property(d => d.BlobUrl).HasColumnName("BlobUrl").IsRequired().HasMaxLength(500);
        b.Property(d => d.Status).HasColumnName("Status").HasConversion<string>().HasColumnType("varchar(15)");
        b.Property(d => d.UploadedUtc).HasColumnName("UploadedUtc");
        b.HasIndex(d => new { d.VendorId, d.Status });
    }
}

public sealed class KycReviewLogConfiguration : IEntityTypeConfiguration<KycReviewLog>
{
    public void Configure(EntityTypeBuilder<KycReviewLog> b)
    {
        b.ToTable("KycReviewLog", Schemas.Vendor);
        b.HasKey(l => l.Id);
        b.Property(l => l.Id).HasColumnName("ReviewLogId").ValueGeneratedOnAdd();
        b.Property(l => l.VendorId).HasColumnName("VendorId");
        b.Property(l => l.Decision).HasColumnName("Decision").HasConversion<string>().HasColumnType("varchar(10)");
        b.Property(l => l.Reason).HasColumnName("Reason").HasMaxLength(500);
        b.Property(l => l.ReviewedBy).HasColumnName("ReviewedBy");
        b.Property(l => l.ReviewedUtc).HasColumnName("ReviewedUtc");
        b.HasIndex(l => new { l.VendorId, l.ReviewedUtc });
    }
}

public sealed class VendorBankAccountConfiguration : IEntityTypeConfiguration<VendorBankAccount>
{
    public void Configure(EntityTypeBuilder<VendorBankAccount> b)
    {
        b.ToTable("VendorBankAccounts", Schemas.Vendor);
        b.HasKey(a => a.Id);
        b.Property(a => a.Id).HasColumnName("BankAccountId").ValueGeneratedOnAdd();
        b.Property(a => a.VendorId).HasColumnName("VendorId");
        b.Property(a => a.AccountHolder).HasColumnName("AccountHolder").IsRequired().HasMaxLength(150);
        b.Property(a => a.BankName).HasColumnName("BankName").IsRequired().HasMaxLength(150);
        b.Property(a => a.AccountNumberEnc).HasColumnName("AccountNumberEnc").HasColumnType("varbinary(256)");
        b.Property(a => a.Ifsc).HasColumnName("IFSC").HasColumnType("varchar(15)");
        b.Property(a => a.IsPrimary).HasColumnName("IsPrimary");
        b.Property(a => a.CreatedUtc).HasColumnName("CreatedUtc");
        b.HasIndex(a => a.VendorId);
    }
}

public sealed class VendorStoreConfiguration : IEntityTypeConfiguration<VendorStore>
{
    public void Configure(EntityTypeBuilder<VendorStore> b)
    {
        b.ToTable("VendorStores", Schemas.Vendor);
        b.HasKey(s => s.Id);
        b.Property(s => s.Id).HasColumnName("StoreId").ValueGeneratedOnAdd();
        b.Property(s => s.VendorId).HasColumnName("VendorId");
        b.HasIndex(s => s.VendorId).IsUnique();

        b.Property(s => s.StoreName).HasColumnName("StoreName").IsRequired().HasMaxLength(150);
        b.Property(s => s.Slug).HasColumnName("Slug").IsRequired().HasMaxLength(160);
        b.HasIndex(s => s.Slug).IsUnique();

        b.Property(s => s.LogoUrl).HasColumnName("LogoUrl").HasMaxLength(500);
        b.Property(s => s.BannerUrl).HasColumnName("BannerUrl").HasMaxLength(500);
        b.Property(s => s.Description).HasColumnName("Description");
        b.Property(s => s.ReturnPolicy).HasColumnName("ReturnPolicy");
        b.Property(s => s.SocialLinks).HasColumnName("SocialLinks");
        b.Property(s => s.IsLive).HasColumnName("IsLive");
        b.Property(s => s.CreatedUtc).HasColumnName("CreatedUtc");

        b.HasMany(s => s.ShippingZones).WithOne().HasForeignKey(z => z.StoreId).OnDelete(DeleteBehavior.Cascade);
        b.Metadata.FindNavigation(nameof(VendorStore.ShippingZones))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class VendorShippingZoneConfiguration : IEntityTypeConfiguration<VendorShippingZone>
{
    public void Configure(EntityTypeBuilder<VendorShippingZone> b)
    {
        b.ToTable("VendorShippingZones", Schemas.Vendor);
        b.HasKey(z => z.Id);
        b.Property(z => z.Id).HasColumnName("ShippingZoneId").ValueGeneratedOnAdd();
        b.Property(z => z.StoreId).HasColumnName("StoreId");
        b.Property(z => z.ZoneName).HasColumnName("ZoneName").IsRequired().HasMaxLength(100);
        b.Property(z => z.Regions).HasColumnName("Regions");
        b.Property(z => z.FlatRate).HasColumnName("FlatRate").HasColumnType("decimal(18,2)");
        b.Property(z => z.FreeAbove).HasColumnName("FreeAbove").HasColumnType("decimal(18,2)");
        b.HasIndex(z => z.StoreId);
    }
}
