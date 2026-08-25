using ShipmentPlatform.Domain.Entities;
using ShipmentPlatform.Domain.Enums;
using ShipmentPlatform.Domain.Exceptions;
using FluentAssertions;

namespace ShipmentPlatform.UnitTests.Domain;

public class ShipmentTests
{
    [Fact]
    public void Create_ShouldStartAsCreated_WithTrackingCode()
    {
        var shipment = Shipment.Create("ACME", "Cliente", "Curitiba", "São Paulo", 12.5m);

        shipment.Status.Should().Be(ShipmentStatus.Created);
        shipment.TrackingCode.Should().StartWith("SP");
        shipment.WeightKg.Should().Be(12.5m);
    }

    [Fact]
    public void Create_WithInvalidWeight_ShouldThrow()
    {
        var act = () => Shipment.Create("ACME", "Cliente", "Curitiba", "São Paulo", 0);

        act.Should().Throw<DomainException>()
            .WithMessage("*Weight*");
    }

    [Fact]
    public void HappyPath_StatusTransitions_ShouldSucceed()
    {
        var shipment = Shipment.Create("ACME", "Cliente", "Curitiba", "São Paulo", 5);

        shipment.MarkPickedUp();
        shipment.Status.Should().Be(ShipmentStatus.PickedUp);

        shipment.MarkInTransit();
        shipment.Status.Should().Be(ShipmentStatus.InTransit);

        shipment.MarkDelivered();
        shipment.Status.Should().Be(ShipmentStatus.Delivered);
    }

    [Fact]
    public void MarkDelivered_FromCreated_ShouldThrow()
    {
        var shipment = Shipment.Create("ACME", "Cliente", "Curitiba", "São Paulo", 5);

        var act = () => shipment.MarkDelivered();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_WhenDelivered_ShouldThrow()
    {
        var shipment = Shipment.Create("ACME", "Cliente", "Curitiba", "São Paulo", 5);
        shipment.MarkPickedUp();
        shipment.MarkInTransit();
        shipment.MarkDelivered();

        var act = () => shipment.Cancel();

        act.Should().Throw<DomainException>();
    }
}
