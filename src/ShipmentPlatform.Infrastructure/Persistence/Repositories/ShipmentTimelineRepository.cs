using Microsoft.EntityFrameworkCore;
using ShipmentPlatform.Application.Abstractions;
using ShipmentPlatform.Application.DTOs;
using ShipmentPlatform.Application.Timeline;
using ShipmentPlatform.Infrastructure.Persistence.Entities;

namespace ShipmentPlatform.Infrastructure.Persistence.Repositories;

public sealed class ShipmentTimelineRepository(AppDbContext db) : IShipmentTimelineRepository
{
    public Task AppendAsync(ShipmentTimelineWrite entry, CancellationToken cancellationToken = default)
    {
        db.ShipmentTimeline.Add(new ShipmentTimelineEntry
        {
            Id = Guid.NewGuid(),
            MessageId = entry.MessageId,
            ShipmentId = entry.ShipmentId,
            TrackingCode = entry.TrackingCode,
            EventType = entry.EventType,
            Description = entry.Description,
            PreviousStatus = entry.PreviousStatus,
            NewStatus = entry.NewStatus,
            OccurredAtUtc = entry.OccurredAtUtc
        });

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ShipmentTimelineEntryResponse>> ListByShipmentIdAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default) =>
        await Project(db.ShipmentTimeline.Where(x => x.ShipmentId == shipmentId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ShipmentTimelineEntryResponse>> ListByTrackingCodeAsync(
        string trackingCode,
        CancellationToken cancellationToken = default) =>
        await Project(db.ShipmentTimeline.Where(x => x.TrackingCode == trackingCode))
            .ToListAsync(cancellationToken);

    private static IQueryable<ShipmentTimelineEntryResponse> Project(IQueryable<ShipmentTimelineEntry> query) =>
        query
            .AsNoTracking()
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => new ShipmentTimelineEntryResponse(
                x.Id,
                x.ShipmentId,
                x.TrackingCode,
                x.EventType,
                x.Description,
                x.PreviousStatus,
                x.NewStatus,
                x.OccurredAtUtc));
}
