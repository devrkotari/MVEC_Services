using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Mvec.BuildingBlocks.Persistence;

/// <summary>
/// EF Core base implementation of <see cref="IRepository{TEntity,TKey}"/>. Concrete per-aggregate
/// repositories derive from this and add their own query methods. The shared
/// <see cref="DbContext"/> instance acts as the unit of work.
/// </summary>
public abstract class EfRepository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class
{
    protected DbContext Db { get; }
    protected DbSet<TEntity> Set => Db.Set<TEntity>();

    protected EfRepository(DbContext db) => Db = db;

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default) =>
        await Set.FindAsync([id], ct);

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) =>
        Set.AnyAsync(predicate, ct);

    public void Add(TEntity entity) => Set.Add(entity);

    public void Remove(TEntity entity) => Set.Remove(entity);
}

/// <summary>Guid-keyed convenience base for <see cref="BaseEntity"/> entities.</summary>
public abstract class EfRepository<TEntity> : EfRepository<TEntity, Guid>, IRepository<TEntity>
    where TEntity : BaseEntity
{
    protected EfRepository(DbContext db) : base(db) { }
}
