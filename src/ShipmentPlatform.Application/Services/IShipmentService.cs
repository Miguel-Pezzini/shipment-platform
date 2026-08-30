using ShipmentPlatform.Application.DTOs;

namespace ShipmentPlatform.Application.Services;

public interface IShipmentService
{
    Task<ShipmentResponse> CreateAsync(CreateShipmentRequest request, CancellationToken cancellationToken = default);
    Task<ShipmentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ShipmentResponse?> GetByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShipmentResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ShipmentResponse?> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShipmentTimelineEntryResponse>?> GetTimelineByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShipmentTimelineEntryResponse>?> GetTimelineByTrackingCodeAsync(
        string trackingCode,
        CancellationToken cancellationToken = default);
}
