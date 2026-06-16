using MassTransit;
using Mvec.Vendor.Api.Application.Abstractions;

namespace Mvec.Vendor.Api.Infrastructure.Messaging;

/// <summary>
/// Publishes integration events directly via MassTransit's <see cref="IPublishEndpoint"/>.
/// (The transactional outbox is not used yet — reintroduce it, with its tables, when consumers exist.)
/// </summary>
public sealed class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitEventPublisher(IPublishEndpoint publishEndpoint) => _publishEndpoint = publishEndpoint;

    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class =>
        _publishEndpoint.Publish(message, ct);
}
