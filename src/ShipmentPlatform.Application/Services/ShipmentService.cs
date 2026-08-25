using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.Caching.Distributed;
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
    IShipmentRepository repository,
    IEventPublisher eventPublisher,
    IValidator<CreateShipmentRequest> validator,
    IDistributedCache cache) : IShipmentService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public async Task<ShipmentResponse> CreateAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var shipment = Shipment.Create(
            request.SenderName,
            request.RecipientName,
            request.OriginCity,
            request.DestinationCity,
            request.WeightKg);

        await repository.AddAsync(shipment, cancellationToken);

        // Publish before SaveChanges so MassTransit EF Outbox shares the same transaction.
        await eventPublisher.PublishAsync(
            new ShipmentCreatedEvent(
                shipment.Id,
                shipment.TrackingCode,
                shipment.OriginCity,
                shipment.DestinationCity,
                DateTime.UtcNow),
            cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);

        var response = shipment.ToResponse();
        await SetCacheAsync(response, cancellationToken);
        return response;
    }

    public async Task<ShipmentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cached = await GetFromCacheAsync(ShipmentCacheKeys.ById(id), cancellationToken);
        if (cached is not null)
            return cached;

        var shipment = await repository.GetByIdAsync(id, cancellationToken);
        if (shipment is null)
            return null;

        var response = shipment.ToResponse();
        await SetCacheAsync(response, cancellationToken);
        return response;
    }

    public async Task<ShipmentResponse?> GetByTrackingCodeAsync(
        string trackingCode,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetFromCacheAsync(ShipmentCacheKeys.ByTracking(trackingCode), cancellationToken);
        if (cached is not null)
            return cached;

        var shipment = await repository.GetByTrackingCodeAsync(trackingCode, cancellationToken);
        if (shipment is null)
            return null;

        var response = shipment.ToResponse();
        await SetCacheAsync(response, cancellationToken);
        return response;
    }

    public async Task<IReadOnlyList<ShipmentResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var shipments = await repository.GetAllAsync(cancellationToken);
        return shipments.Select(s => s.ToResponse()).ToList();
    }

    public async Task<ShipmentResponse?> UpdateStatusAsync(
        Guid id,
        string status,
        CancellationToken cancellationToken = default)
    {
        var shipment = await repository.GetByIdAsync(id, cancellationToken);
        if (shipment is null)
            return null;

        if (!Enum.TryParse<ShipmentStatus>(status, ignoreCase: true, out var parsed))
            throw new DomainException($"Invalid status '{status}'.");

        switch (parsed)
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
                throw new DomainException($"Status '{parsed}' cannot be set directly.");
        }

        await repository.SaveChangesAsync(cancellationToken);

        var response = shipment.ToResponse();
        await SetCacheAsync(response, cancellationToken);
        return response;
    }

    private async Task<ShipmentResponse?> GetFromCacheAsync(string key, CancellationToken cancellationToken)
    {
        var bytes = await cache.GetAsync(key, cancellationToken);
        if (bytes is null || bytes.Length == 0)
            return null;

        return JsonSerializer.Deserialize<ShipmentResponse>(bytes, JsonOptions);
    }

    private async Task SetCacheAsync(ShipmentResponse response, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(response, JsonOptions);
        await cache.SetAsync(ShipmentCacheKeys.ById(response.Id), payload, CacheOptions, cancellationToken);
        await cache.SetAsync(
            ShipmentCacheKeys.ByTracking(response.TrackingCode),
            payload,
            CacheOptions,
            cancellationToken);
    }
}
