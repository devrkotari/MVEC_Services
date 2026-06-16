using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mvec.Vendor.Api.Infrastructure;

/// <summary>
/// Used by EF Core tooling so it can build the context without running the application's
/// startup pipeline. (Database-first: no migrations are generated — this is for design-time only.)
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<VendorDbContext>
{
    public VendorDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<VendorDbContext>()
            .UseSqlServer("Server=RAMKOTARI-41MFC;Database=Mvec_Vendor;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new VendorDbContext(options);
    }
}
