using Microsoft.EntityFrameworkCore;
using ShipmentPlatform.Application.Abstractions;
using ShipmentPlatform.Domain.Entities;
using ShipmentPlatform.Infrastructure.Persistence;

namespace ShipmentPlatform.Infrastructure.Persistence.Repositories;

public class ShipmentRepository(AppDbContext db) : IShipmentRepository
{
    public async Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default) =>
        await db.Shipments.AddAsync(shipment, cancellationToken);

    public Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Shipments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Shipment?> GetByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken = default) =>
        db.Shipments.FirstOrDefaultAsync(x => x.TrackingCode == trackingCode, cancellationToken);

    public async Task<IReadOnlyList<Shipment>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Shipments
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
