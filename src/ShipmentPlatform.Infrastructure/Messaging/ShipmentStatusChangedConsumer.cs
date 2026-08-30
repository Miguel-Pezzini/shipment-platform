using MassTransit;
using Microsoft.Extensions.Logging;
using ShipmentPlatform.Application.Events;

namespace ShipmentPlatform.Infrastructure.Messaging;

public sealed class ShipmentStatusChangedConsumer(
    ILogger<ShipmentStatusChangedConsumer> logger,
    InboxGuard inbox) : IConsumer<ShipmentStatusChangedEvent>
{
    public async Task Consume(ConsumeContext<ShipmentStatusChangedEvent> context)
    {
        if (context.MessageId is not { } messageId)
        {
            logger.LogWarning("ShipmentStatusChanged skipped inbox: missing MessageId");
        }
        else if (!await inbox.TryClaimAsync(messageId, nameof(ShipmentStatusChangedConsumer), context.CancellationToken))
        {
            logger.LogInformation("ShipmentStatusChanged duplicate ignored: {MessageId}", messageId);
            return;
        }

        var message = context.Message;
        logger.LogInformation(
            "ShipmentStatusChanged consumed: {ShipmentId} {TrackingCode} {Previous} -> {New}",
            message.ShipmentId,
            message.TrackingCode,
            message.PreviousStatus,
            message.NewStatus);
    }
}
