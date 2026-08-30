using MassTransit;
using Microsoft.Extensions.Logging;
using ShipmentPlatform.Application.Abstractions;
using ShipmentPlatform.Application.Events;
using ShipmentPlatform.Application.Timeline;

namespace ShipmentPlatform.Infrastructure.Messaging;

public sealed class ShipmentCreatedConsumer(
    ILogger<ShipmentCreatedConsumer> logger,
    InboxGuard inbox,
    IShipmentTimelineRepository timeline) : IConsumer<ShipmentCreatedEvent>
{
    public async Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
    {
        if (context.MessageId is not { } messageId)
        {
            logger.LogWarning("ShipmentCreated skipped: missing MessageId");
            return;
        }

        var message = context.Message;
        var claimed = await inbox.TryClaimAsync(
            messageId,
            nameof(ShipmentCreatedConsumer),
            ct => timeline.AppendAsync(ShipmentTimeline.FromCreated(message, messageId), ct),
            context.CancellationToken);

        if (!claimed)
        {
            logger.LogInformation("ShipmentCreated duplicate ignored: {MessageId}", messageId);
            return;
        }

        logger.LogInformation(
            "ShipmentCreated projected: {ShipmentId} {TrackingCode} {Origin} -> {Destination}",
            message.ShipmentId,
            message.TrackingCode,
            message.OriginCity,
            message.DestinationCity);
    }
}
