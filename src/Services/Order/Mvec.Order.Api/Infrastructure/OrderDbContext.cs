using Microsoft.EntityFrameworkCore;
using Mvec.Order.Api.Domain;

namespace Mvec.Order.Api.Infrastructure;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: entity configurations + MassTransit outbox (AddTransactionalOutboxEntities)
        base.OnModelCreating(modelBuilder);
    }
}
