using FluentValidation;
using ShipmentPlatform.Application.Abstractions;
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
    IValidator<CreateShipmentRequest> validator) : IShipmentService
{
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
        await repository.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync(
            new ShipmentCreatedEvent(
                shipment.Id,
                shipment.TrackingCode,
                shipment.OriginCity,
                shipment.DestinationCity,
                DateTime.UtcNow),
            cancellationToken);

        return shipment.ToResponse();
    }

    public async Task<ShipmentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shipment = await repository.GetByIdAsync(id, cancellationToken);
        return shipment?.ToResponse();
    }

    public async Task<ShipmentResponse?> GetByTrackingCodeAsync(
        string trackingCode,
        CancellationToken cancellationToken = default)
    {
        var shipment = await repository.GetByTrackingCodeAsync(trackingCode, cancellationToken);
        return shipment?.ToResponse();
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
        return shipment.ToResponse();
    }
}
