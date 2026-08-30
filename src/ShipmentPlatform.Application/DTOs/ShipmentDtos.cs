namespace ShipmentPlatform.Application.DTOs;

public record CreateShipmentRequest(
    string SenderName,
    string RecipientName,
    string OriginCity,
    string DestinationCity,
    decimal WeightKg);

public record ShipmentResponse(
    Guid Id,
    string TrackingCode,
    string SenderName,
    string RecipientName,
    string OriginCity,
    string DestinationCity,
    decimal WeightKg,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public record UpdateShipmentStatusRequest(string Status);

public record ShipmentTimelineEntryResponse(
    Guid Id,
    Guid ShipmentId,
    string TrackingCode,
    string EventType,
    string Description,
    string? PreviousStatus,
    string? NewStatus,
    DateTime OccurredAtUtc);
