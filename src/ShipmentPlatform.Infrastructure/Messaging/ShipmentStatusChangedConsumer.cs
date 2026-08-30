using MassTransit;
using Microsoft.Extensions.Logging;
using ShipmentPlatform.Application.Abstractions;
using ShipmentPlatform.Application.Events;
using ShipmentPlatform.Application.Timeline;

namespace ShipmentPlatform.Infrastructure.Messaging;

public sealed class ShipmentStatusChangedConsumer(
    ILogger<ShipmentStatusChangedConsumer> logger,
    InboxGuard inbox,
    IShipmentTimelineRepository timeline) : IConsumer<ShipmentStatusChangedEvent>
{
    public async Task Consume(ConsumeContext<ShipmentStatusChangedEvent> context)
    {
        if (context.MessageId is not { } messageId)
        {
            logger.LogWarning("ShipmentStatusChanged skipped: missing MessageId");
            return;
        }

        var message = context.Message;
        var claimed = await inbox.TryClaimAsync(
            messageId,
            nameof(ShipmentStatusChangedConsumer),
            ct => timeline.AppendAsync(ShipmentTimeline.FromStatusChanged(message, messageId), ct),
            context.CancellationToken);

        if (!claimed)
        {
            logger.LogInformation("ShipmentStatusChanged duplicate ignored: {MessageId}", messageId);
            return;
        }

        logger.LogInformation(
            "ShipmentStatusChanged projected: {ShipmentId} {TrackingCode} {Previous} -> {New}",
            message.ShipmentId,
            message.TrackingCode,
            message.PreviousStatus,
            message.NewStatus);
    }
}
