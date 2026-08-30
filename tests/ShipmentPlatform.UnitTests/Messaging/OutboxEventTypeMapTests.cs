using FluentAssertions;
using ShipmentPlatform.Application.Events;
using ShipmentPlatform.Infrastructure.Messaging;

namespace ShipmentPlatform.UnitTests.Messaging;

public class OutboxEventTypeMapTests
{
    [Fact]
    public void GetTypeName_AndTryGetType_RoundTripKnownEvents()
    {
        var createdName = OutboxEventTypeMap.GetTypeName(typeof(ShipmentCreatedEvent));
        var statusName = OutboxEventTypeMap.GetTypeName(typeof(ShipmentStatusChangedEvent));

        createdName.Should().Be(typeof(ShipmentCreatedEvent).FullName);
        statusName.Should().Be(typeof(ShipmentStatusChangedEvent).FullName);

        OutboxEventTypeMap.TryGetType(createdName, out var createdType).Should().BeTrue();
        createdType.Should().Be(typeof(ShipmentCreatedEvent));

        OutboxEventTypeMap.TryGetType(statusName, out var statusType).Should().BeTrue();
        statusType.Should().Be(typeof(ShipmentStatusChangedEvent));
    }

    [Fact]
    public void TryGetType_WhenUnknown_ShouldReturnFalse()
    {
        OutboxEventTypeMap.TryGetType("Unknown.Event.Type", out var type).Should().BeFalse();
        type.Should().BeNull();
    }
}
