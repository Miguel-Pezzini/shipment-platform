namespace ShipmentPlatform.Application.Abstractions;

public interface IShipmentRepository
{
    Task AddAsync(Domain.Entities.Shipment shipment, CancellationToken cancellationToken = default);
    Task<Domain.Entities.Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Domain.Entities.Shipment?> GetByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Domain.Entities.Shipment> Items, int TotalCount)> ListPagedAsync(
        int page,
        int perPage,
        CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
