using Microsoft.EntityFrameworkCore;
using Mvec.Product.Api.Domain;

namespace Mvec.Product.Api.Infrastructure;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: entity configurations + MassTransit outbox (AddTransactionalOutboxEntities)
        base.OnModelCreating(modelBuilder);
    }
}
