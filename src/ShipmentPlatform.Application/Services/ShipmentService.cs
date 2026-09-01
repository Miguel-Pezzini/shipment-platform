using ShipmentPlatform.Application.Abstractions;
using ShipmentPlatform.Application.Caching;
using ShipmentPlatform.Application.DTOs;
using ShipmentPlatform.Application.Events;
using ShipmentPlatform.Application.Mappings;
using ShipmentPlatform.Domain.Entities;
using ShipmentPlatform.Domain.Enums;
using ShipmentPlatform.Domain.Exceptions;

namespace ShipmentPlatform.Application.Services;

public class ShipmentService(
    IShipmentRepository shipmentRepository,
    IShipmentTimelineRepository timelineRepository,
    IEventPublisher eventPublisher,
    ICache cache) : IShipmentService
{
    public async Task<ShipmentResponse> CreateAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var shipment = Shipment.Create(
            request.SenderName,
            request.RecipientName,
            request.OriginCity,
            request.DestinationCity,
            request.WeightKg);

        await shipmentRepository.AddAsync(shipment, cancellationToken);

        await PublishShipmentCreatedAsync(shipment, cancellationToken);

        await shipmentRepository.SaveChangesAsync(cancellationToken);

        return await MapToResponseAndCacheAsync(shipment, cancellationToken);
    }

    public Task<ShipmentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetFromCacheOrDatabaseAsync(
            ShipmentCacheKeys.ById(id),
            ct => shipmentRepository.GetByIdAsync(id, ct),
            cancellationToken);

    public Task<ShipmentResponse?> GetByTrackingCodeAsync(
        string trackingCode,
        CancellationToken cancellationToken = default) =>
        GetFromCacheOrDatabaseAsync(
            ShipmentCacheKeys.ByTracking(trackingCode),
            ct => shipmentRepository.GetByTrackingCodeAsync(trackingCode, ct),
            cancellationToken);

    public async Task<PagedResult<ShipmentResponse>> GetAllAsync(
        PagedQuery query,
        CancellationToken cancellationToken = default)
    {
        var (shipments, totalCount) = await shipmentRepository.ListPagedAsync(
            query.Page,
            query.PerPage,
            cancellationToken);

        return PagedResult<ShipmentResponse>.Create(
            shipments.Select(shipment => shipment.ToResponse()).ToList(),
            query.Page,
            query.PerPage,
            totalCount);
    }

    public async Task<IReadOnlyList<ShipmentTimelineEntryResponse>?> GetTimelineByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var shipment = await shipmentRepository.GetByIdAsync(id, cancellationToken);
        if (shipment is null)
            return null;

        return await timelineRepository.ListByShipmentIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<ShipmentTimelineEntryResponse>?> GetTimelineByTrackingCodeAsync(
        string trackingCode,
        CancellationToken cancellationToken = default)
    {
        var shipment = await shipmentRepository.GetByTrackingCodeAsync(trackingCode, cancellationToken);
        if (shipment is null)
            return null;

        return await timelineRepository.ListByTrackingCodeAsync(trackingCode, cancellationToken);
    }

    public async Task<ShipmentResponse?> UpdateStatusAsync(
        Guid id,
        string status,
        CancellationToken cancellationToken = default)
    {
        var shipment = await shipmentRepository.GetByIdAsync(id, cancellationToken);
        if (shipment is null)
            return null;

        var requestedStatus = ParseRequestedStatus(status);
        var previousStatus = shipment.Status;

        ApplyStatusTransition(shipment, requestedStatus);

        await PublishStatusChangedAsync(shipment, previousStatus, cancellationToken);
        await shipmentRepository.SaveChangesAsync(cancellationToken);

        return await InvalidateAndCacheAsync(shipment, cancellationToken);
    }

    private async Task<ShipmentResponse?> GetFromCacheOrDatabaseAsync(
        string cacheKey,
        Func<CancellationToken, Task<Shipment?>> loadFromDatabase,
        CancellationToken cancellationToken)
    {
        var cachedShipment = await cache.GetAsync<ShipmentResponse>(cacheKey, cancellationToken);
        if (cachedShipment is not null)
            return cachedShipment;

        var shipment = await loadFromDatabase(cancellationToken);
        if (shipment is null)
            return null;

        return await MapToResponseAndCacheAsync(shipment, cancellationToken);
    }

    private async Task<ShipmentResponse> MapToResponseAndCacheAsync(
        Shipment shipment,
        CancellationToken cancellationToken)
    {
        var response = shipment.ToResponse();
        await CacheShipmentAsync(response, cancellationToken);
        return response;
    }

    private async Task<ShipmentResponse> InvalidateAndCacheAsync(
        Shipment shipment,
        CancellationToken cancellationToken)
    {
        var response = shipment.ToResponse();
        await cache.RemoveAsync(ShipmentCacheKeys.For(response.Id, response.TrackingCode), cancellationToken);
        await CacheShipmentAsync(response, cancellationToken);
        return response;
    }

    private async Task CacheShipmentAsync(ShipmentResponse response, CancellationToken cancellationToken)
    {
        foreach (var cacheKey in ShipmentCacheKeys.For(response.Id, response.TrackingCode))
        {
            await cache.SetAsync(cacheKey, response, cancellationToken: cancellationToken);
        }
    }

    private static ShipmentStatus ParseRequestedStatus(string status)
    {
        if (!Enum.TryParse<ShipmentStatus>(status, ignoreCase: true, out var requestedStatus))
            throw new DomainException($"Invalid status '{status}'.");

        return requestedStatus;
    }

    private static void ApplyStatusTransition(Shipment shipment, ShipmentStatus requestedStatus)
    {
        switch (requestedStatus)
        {
            case ShipmentStatus.PickedUp:
                shipment.MarkPickedUp();
                break;
            case ShipmentStatus.InTransit:
                shipment.MarkInTransit();
                break;
            case ShipmentStatus.Delivered:
                shipment.MarkDelivered();
                break;
            case ShipmentStatus.Cancelled:
                shipment.Cancel();
                break;
            default:
                throw new DomainException($"Status '{requestedStatus}' cannot be set directly.");
        }
    }

    private Task PublishShipmentCreatedAsync(Shipment shipment, CancellationToken cancellationToken) =>
        eventPublisher.PublishAsync(
            new ShipmentCreatedEvent(
                shipment.Id,
                shipment.TrackingCode,
                shipment.OriginCity,
                shipment.DestinationCity,
                DateTime.UtcNow),
            cancellationToken);

    private Task PublishStatusChangedAsync(
        Shipment shipment,
        ShipmentStatus previousStatus,
        CancellationToken cancellationToken) =>
        eventPublisher.PublishAsync(
            new ShipmentStatusChangedEvent(
                shipment.Id,
                shipment.TrackingCode,
                previousStatus.ToString(),
                shipment.Status.ToString(),
                DateTime.UtcNow),
            cancellationToken);
}
