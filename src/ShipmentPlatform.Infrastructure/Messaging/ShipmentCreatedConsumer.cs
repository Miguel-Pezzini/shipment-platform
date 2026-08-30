using MassTransit;
using Microsoft.Extensions.Logging;
using ShipmentPlatform.Application.Events;

namespace ShipmentPlatform.Infrastructure.Messaging;

public sealed class ShipmentCreatedConsumer(
    ILogger<ShipmentCreatedConsumer> logger,
    InboxGuard inbox) : IConsumer<ShipmentCreatedEvent>
{
    public async Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
    {
        if (context.MessageId is not { } messageId)
        {
            logger.LogWarning("ShipmentCreated skipped inbox: missing MessageId");
        }
        else if (!await inbox.TryClaimAsync(messageId, nameof(ShipmentCreatedConsumer), context.CancellationToken))
        {
            logger.LogInformation("ShipmentCreated duplicate ignored: {MessageId}", messageId);
            return;
        }

        var message = context.Message;
        logger.LogInformation(
            "ShipmentCreated consumed: {ShipmentId} {TrackingCode} {Origin} -> {Destination}",
            message.ShipmentId,
            message.TrackingCode,
            message.OriginCity,
            message.DestinationCity);
    }
}
