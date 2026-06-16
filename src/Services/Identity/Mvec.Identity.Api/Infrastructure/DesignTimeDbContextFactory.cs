using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mvec.Identity.Api.Infrastructure;

/// <summary>
/// Used by EF Core tooling (<c>dotnet ef migrations</c>) so it can build the context
/// without running the application's startup/seeding pipeline.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer("Server=RAMKOTARI-41MFC;Database=Mvec_Identity;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new IdentityDbContext(options);
    }
}
