namespace Mvec.BuildingBlocks.Persistence;

/// <summary>
/// Commits all changes tracked across repositories in a single transaction. Implemented by
/// each service's DbContext so repositories and the transactional outbox commit atomically.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
