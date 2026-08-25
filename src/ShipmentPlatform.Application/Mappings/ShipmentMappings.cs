using ShipmentPlatform.Application.DTOs;
using ShipmentPlatform.Domain.Entities;

namespace ShipmentPlatform.Application.Mappings;

public static class ShipmentMappings
{
    public static ShipmentResponse ToResponse(this Shipment shipment) =>
        new(
            shipment.Id,
            shipment.TrackingCode,
            shipment.SenderName,
            shipment.RecipientName,
            shipment.OriginCity,
            shipment.DestinationCity,
            shipment.WeightKg,
            shipment.Status.ToString(),
            shipment.CreatedAtUtc,
            shipment.UpdatedAtUtc);
}
