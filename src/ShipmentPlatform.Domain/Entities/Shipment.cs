using ShipmentPlatform.Domain.Enums;
using ShipmentPlatform.Domain.Exceptions;

namespace ShipmentPlatform.Domain.Entities;

public class Shipment
{
    public Guid Id { get; private set; }
    public string TrackingCode { get; private set; } = string.Empty;
    public string SenderName { get; private set; } = string.Empty;
    public string RecipientName { get; private set; } = string.Empty;
    public string OriginCity { get; private set; } = string.Empty;
    public string DestinationCity { get; private set; } = string.Empty;
    public decimal WeightKg { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private Shipment()
    {
    }

    public static Shipment Create(
        string senderName,
        string recipientName,
        string originCity,
        string destinationCity,
        decimal weightKg)
    {
        if (string.IsNullOrWhiteSpace(senderName))
            throw new DomainException("Sender name is required.");

        if (string.IsNullOrWhiteSpace(recipientName))
            throw new DomainException("Recipient name is required.");

        if (string.IsNullOrWhiteSpace(originCity))
            throw new DomainException("Origin city is required.");

        if (string.IsNullOrWhiteSpace(destinationCity))
            throw new DomainException("Destination city is required.");

        if (weightKg <= 0)
            throw new DomainException("Weight must be greater than zero.");

        var now = DateTime.UtcNow;

        return new Shipment
        {
            Id = Guid.NewGuid(),
            TrackingCode = GenerateTrackingCode(),
            SenderName = senderName.Trim(),
            RecipientName = recipientName.Trim(),
            OriginCity = originCity.Trim(),
            DestinationCity = destinationCity.Trim(),
            WeightKg = weightKg,
            Status = ShipmentStatus.Created,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void MarkPickedUp() => TransitionTo(ShipmentStatus.PickedUp, ShipmentStatus.Created);

    public void MarkInTransit() => TransitionTo(ShipmentStatus.InTransit, ShipmentStatus.PickedUp);

    public void MarkDelivered() => TransitionTo(ShipmentStatus.Delivered, ShipmentStatus.InTransit);

    public void Cancel()
    {
        if (Status is ShipmentStatus.Delivered or ShipmentStatus.Cancelled)
            throw new DomainException($"Cannot cancel a shipment with status {Status}.");

        Status = ShipmentStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void TransitionTo(ShipmentStatus next, ShipmentStatus expectedCurrent)
    {
        if (Status != expectedCurrent)
            throw new DomainException($"Cannot move from {Status} to {next}.");

        Status = next;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string GenerateTrackingCode() =>
        $"SP{DateTime.UtcNow:yyMMdd}{Random.Shared.Next(100000, 999999)}";
}
