using Microsoft.EntityFrameworkCore;
using Mvec.Review.Api.Domain;

namespace Mvec.Review.Api.Infrastructure;

public class ReviewDbContext : DbContext
{
    public ReviewDbContext(DbContextOptions<ReviewDbContext> options) : base(options) { }

    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: entity configurations + MassTransit outbox (AddTransactionalOutboxEntities)
        base.OnModelCreating(modelBuilder);
    }
}
