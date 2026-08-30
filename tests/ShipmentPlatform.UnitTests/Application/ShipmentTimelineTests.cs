using FluentAssertions;
using ShipmentPlatform.Application.Events;
using ShipmentPlatform.Application.Timeline;

namespace ShipmentPlatform.UnitTests.Application;

public class ShipmentTimelineTests
{
    [Fact]
    public void FromCreated_ShouldDescribeOriginAndDestination()
    {
        var occurredAt = DateTime.UtcNow;
        var messageId = Guid.NewGuid();
        var @event = new ShipmentCreatedEvent(
            Guid.NewGuid(),
            "SP240101123456",
            "Curitiba",
            "Florianópolis",
            occurredAt);

        var write = ShipmentTimeline.FromCreated(@event, messageId);

        write.MessageId.Should().Be(messageId);
        write.ShipmentId.Should().Be(@event.ShipmentId);
        write.EventType.Should().Be(ShipmentTimeline.Created);
        write.Description.Should().Be("Shipment created: Curitiba → Florianópolis");
        write.PreviousStatus.Should().BeNull();
        write.NewStatus.Should().BeNull();
        write.OccurredAtUtc.Should().Be(occurredAt);
    }

    [Fact]
    public void FromStatusChanged_ShouldDescribeTransition()
    {
        var @event = new ShipmentStatusChangedEvent(
            Guid.NewGuid(),
            "SP240101123456",
            "Created",
            "PickedUp",
            DateTime.UtcNow);

        var write = ShipmentTimeline.FromStatusChanged(@event, Guid.NewGuid());

        write.EventType.Should().Be(ShipmentTimeline.StatusChanged);
        write.Description.Should().Be("Status changed from Created to PickedUp");
        write.PreviousStatus.Should().Be("Created");
        write.NewStatus.Should().Be("PickedUp");
    }
}
