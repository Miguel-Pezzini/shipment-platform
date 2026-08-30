using ShipmentPlatform.Application.DTOs;
using ShipmentPlatform.Application.Timeline;

namespace ShipmentPlatform.Application.Abstractions;

public interface IShipmentTimelineRepository
{
    Task AppendAsync(ShipmentTimelineWrite entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShipmentTimelineEntryResponse>> ListByShipmentIdAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShipmentTimelineEntryResponse>> ListByTrackingCodeAsync(
        string trackingCode,
        CancellationToken cancellationToken = default);
}
