using MassTransit;
using Microsoft.Extensions.Logging;
using ShipmentPlatform.Application.Events;

namespace ShipmentPlatform.Infrastructure.Messaging;

public sealed class ShipmentStatusChangedConsumer(ILogger<ShipmentStatusChangedConsumer> logger)
    : IConsumer<ShipmentStatusChangedEvent>
{
    public Task Consume(ConsumeContext<ShipmentStatusChangedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation(
            "ShipmentStatusChanged consumed: {ShipmentId} {TrackingCode} {Previous} -> {New}",
            message.ShipmentId,
            message.TrackingCode,
            message.PreviousStatus,
            message.NewStatus);

        return Task.CompletedTask;
    }
}
