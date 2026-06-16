using Microsoft.EntityFrameworkCore;
using Mvec.BuildingBlocks.Persistence;
using Mvec.Vendor.Api.Domain;
using Mvec.Vendor.Api.Infrastructure.Configurations;

namespace Mvec.Vendor.Api.Infrastructure;

// Database-first: maps to the existing vnd.* schema (owned by Database/Schema.sql). EF does not
// create or migrate these tables. The DbContext is the unit-of-work commit boundary.
public class VendorDbContext : DbContext, IUnitOfWork
{
    public VendorDbContext(DbContextOptions<VendorDbContext> options) : base(options) { }

    public DbSet<Domain.Vendor> Vendors => Set<Domain.Vendor>();
    public DbSet<VendorKycDocument> KycDocuments => Set<VendorKycDocument>();
    public DbSet<KycReviewLog> KycReviewLogs => Set<KycReviewLog>();
    public DbSet<VendorBankAccount> BankAccounts => Set<VendorBankAccount>();
    public DbSet<VendorStore> Stores => Set<VendorStore>();
    public DbSet<VendorShippingZone> ShippingZones => Set<VendorShippingZone>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new VendorConfiguration());
        modelBuilder.ApplyConfiguration(new VendorKycDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new KycReviewLogConfiguration());
        modelBuilder.ApplyConfiguration(new VendorBankAccountConfiguration());
        modelBuilder.ApplyConfiguration(new VendorStoreConfiguration());
        modelBuilder.ApplyConfiguration(new VendorShippingZoneConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
