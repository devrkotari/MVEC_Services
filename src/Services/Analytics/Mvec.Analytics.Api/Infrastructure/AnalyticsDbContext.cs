using Microsoft.EntityFrameworkCore;
using Mvec.Analytics.Api.Domain;

namespace Mvec.Analytics.Api.Infrastructure;

public class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options) { }

    public DbSet<SalesFact> SalesFacts => Set<SalesFact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: entity configurations + MassTransit outbox (AddTransactionalOutboxEntities)
        base.OnModelCreating(modelBuilder);
    }
}
