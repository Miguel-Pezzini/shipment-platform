using FluentAssertions;
using Moq;
using ShipmentPlatform.Application.Abstractions;
using ShipmentPlatform.Application.DTOs;
using ShipmentPlatform.Application.Events;
using ShipmentPlatform.Application.Services;
using ShipmentPlatform.Application.Validators;
using ShipmentPlatform.Domain.Entities;
using ShipmentPlatform.Domain.Exceptions;
using ShipmentPlatform.UnitTests.Fakes;

namespace ShipmentPlatform.UnitTests.Application;

public class ShipmentServiceTests
{
    private readonly Mock<IShipmentRepository> _repository = new();
    private readonly Mock<IEventPublisher> _publisher = new();
    private readonly ICache _cache = new InMemoryCache();
    private readonly ShipmentService _sut;

    public ShipmentServiceTests()
    {
        _sut = new ShipmentService(
            _repository.Object,
            _publisher.Object,
            new CreateShipmentRequestValidator(),
            _cache);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistPublishAndCache()
    {
        var request = new CreateShipmentRequest("ACME", "Cliente", "Curitiba", "São Paulo", 8);

        Shipment? saved = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Shipment>(), It.IsAny<CancellationToken>()))
            .Callback<Shipment, CancellationToken>((s, _) => saved = s)
            .Returns(Task.CompletedTask);

        _repository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _sut.CreateAsync(request);

        response.SenderName.Should().Be("ACME");
        response.Status.Should().Be("Created");
        saved.Should().NotBeNull();

        _publisher.Verify(
            p => p.PublishAsync(It.IsAny<ShipmentCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);

        var cached = await _sut.GetByIdAsync(response.Id);
        cached.Should().NotBeNull();
        cached!.TrackingCode.Should().Be(response.TrackingCode);
        _repository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldPublishEventAndRefreshCache()
    {
        var shipment = Shipment.Create("ACME", "Cliente", "Curitiba", "São Paulo", 8);

        _repository
            .Setup(r => r.GetByIdAsync(shipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shipment);

        _repository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _sut.UpdateStatusAsync(shipment.Id, "PickedUp");

        response.Should().NotBeNull();
        response!.Status.Should().Be("PickedUp");

        _publisher.Verify(
            p => p.PublishAsync(
                It.Is<ShipmentStatusChangedEvent>(e =>
                    e.ShipmentId == shipment.Id
                    && e.PreviousStatus == "Created"
                    && e.NewStatus == "PickedUp"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var cached = await _sut.GetByIdAsync(shipment.Id);
        cached!.Status.Should().Be("PickedUp");
        _repository.Verify(r => r.GetByIdAsync(shipment.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenTransitionIsInvalid_ShouldNotPublish()
    {
        var shipment = Shipment.Create("ACME", "Cliente", "Curitiba", "São Paulo", 8);

        _repository
            .Setup(r => r.GetByIdAsync(shipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shipment);

        var act = () => _sut.UpdateStatusAsync(shipment.Id, "Delivered");

        await act.Should().ThrowAsync<DomainException>();
        _publisher.Verify(
            p => p.PublishAsync(It.IsAny<ShipmentStatusChangedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
