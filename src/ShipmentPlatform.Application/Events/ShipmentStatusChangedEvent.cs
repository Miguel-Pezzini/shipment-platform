namespace ShipmentPlatform.Application.Events;

public record ShipmentStatusChangedEvent(
    Guid ShipmentId,
    string TrackingCode,
    string PreviousStatus,
    string NewStatus,
    DateTime OccurredAtUtc);
