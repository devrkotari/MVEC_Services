using System.Linq.Expressions;

namespace Mvec.BuildingBlocks.Persistence;

/// <summary>
/// Generic persistence contract for an aggregate root with key type <typeparamref name="TKey"/>.
/// Application code depends on repository interfaces (+ <see cref="IUnitOfWork"/>), never the DbContext.
/// </summary>
public interface IRepository<TEntity, in TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    void Add(TEntity entity);
    void Remove(TEntity entity);
}

/// <summary>Convenience contract for Guid-keyed entities based on <see cref="BaseEntity"/>.</summary>
public interface IRepository<TEntity> : IRepository<TEntity, Guid> where TEntity : BaseEntity;
