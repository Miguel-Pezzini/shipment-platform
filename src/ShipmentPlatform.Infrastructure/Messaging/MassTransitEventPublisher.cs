using MassTransit;
using ShipmentPlatform.Application.Abstractions;

namespace ShipmentPlatform.Infrastructure.Messaging;

public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class =>
        publishEndpoint.Publish(@event, cancellationToken);
}
