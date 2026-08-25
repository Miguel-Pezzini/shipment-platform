namespace ShipmentPlatform.Application.Events;

public record ShipmentCreatedEvent(
    Guid ShipmentId,
    string TrackingCode,
    string OriginCity,
    string DestinationCity,
    DateTime OccurredAtUtc);
