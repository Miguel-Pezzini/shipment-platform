using MassTransit;
using Microsoft.Extensions.Logging;
using ShipmentPlatform.Application.Events;

namespace ShipmentPlatform.Infrastructure.Messaging;

public sealed class ShipmentCreatedConsumer(ILogger<ShipmentCreatedConsumer> logger)
    : IConsumer<ShipmentCreatedEvent>
{
    public Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation(
            "ShipmentCreated consumed: {ShipmentId} {TrackingCode} {Origin} -> {Destination}",
            message.ShipmentId,
            message.TrackingCode,
            message.OriginCity,
            message.DestinationCity);

        return Task.CompletedTask;
    }
}
