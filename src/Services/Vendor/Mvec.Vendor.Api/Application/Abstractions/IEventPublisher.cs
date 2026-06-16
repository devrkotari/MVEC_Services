namespace Mvec.Vendor.Api.Application.Abstractions;

/// <summary>
/// Publishes integration events (contracts in <c>Mvec.Contracts</c>). Backed by MassTransit;
/// reintroduce the transactional outbox (with its tables) once downstream consumers exist.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
}
