using ShipmentPlatform.Application.Events;

namespace ShipmentPlatform.Application.Timeline;

public static class ShipmentTimeline
{
    public const string Created = "Created";
    public const string StatusChanged = "StatusChanged";

    public static ShipmentTimelineWrite FromCreated(ShipmentCreatedEvent @event, Guid messageId) =>
        new(
            MessageId: messageId,
            ShipmentId: @event.ShipmentId,
            TrackingCode: @event.TrackingCode,
            EventType: Created,
            Description: $"Shipment created: {@event.OriginCity} → {@event.DestinationCity}",
            OccurredAtUtc: @event.OccurredAtUtc);

    public static ShipmentTimelineWrite FromStatusChanged(ShipmentStatusChangedEvent @event, Guid messageId) =>
        new(
            MessageId: messageId,
            ShipmentId: @event.ShipmentId,
            TrackingCode: @event.TrackingCode,
            EventType: StatusChanged,
            Description: $"Status changed from {@event.PreviousStatus} to {@event.NewStatus}",
            OccurredAtUtc: @event.OccurredAtUtc,
            PreviousStatus: @event.PreviousStatus,
            NewStatus: @event.NewStatus);
}

public sealed record ShipmentTimelineWrite(
    Guid MessageId,
    Guid ShipmentId,
    string TrackingCode,
    string EventType,
    string Description,
    DateTime OccurredAtUtc,
    string? PreviousStatus = null,
    string? NewStatus = null);
