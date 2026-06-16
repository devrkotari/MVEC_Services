using Microsoft.EntityFrameworkCore;
using Mvec.Payment.Api.Domain;

namespace Mvec.Payment.Api.Infrastructure;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: entity configurations + MassTransit outbox (AddTransactionalOutboxEntities)
        base.OnModelCreating(modelBuilder);
    }
}
