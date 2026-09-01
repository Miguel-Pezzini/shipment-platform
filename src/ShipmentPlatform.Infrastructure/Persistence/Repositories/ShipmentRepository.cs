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

    public async Task<(IReadOnlyList<Shipment> Items, int TotalCount)> ListPagedAsync(
        int page,
        int perPage,
        CancellationToken cancellationToken = default)
    {
        var query = db.Shipments
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
