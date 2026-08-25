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
