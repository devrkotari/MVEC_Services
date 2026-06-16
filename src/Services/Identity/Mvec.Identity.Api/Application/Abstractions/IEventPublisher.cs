namespace Mvec.Identity.Api.Application.Abstractions;

/// <summary>
/// Publishes integration events. Backed by the MassTransit transactional outbox so
/// messages are committed atomically with the owning database transaction.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
}
