using ShipmentPlatform.Application.Abstractions;
using ShipmentPlatform.Application.DTOs;
using ShipmentPlatform.Application.Events;
using ShipmentPlatform.Application.Services;
using ShipmentPlatform.Application.Validators;
using ShipmentPlatform.Domain.Entities;
using FluentAssertions;
using Moq;

namespace ShipmentPlatform.UnitTests.Application;

public class ShipmentServiceTests
{
    private readonly Mock<IShipmentRepository> _repository = new();
    private readonly Mock<IEventPublisher> _publisher = new();
    private readonly ShipmentService _sut;

    public ShipmentServiceTests()
    {
        _sut = new ShipmentService(
            _repository.Object,
            _publisher.Object,
            new CreateShipmentRequestValidator());
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistAndPublishEvent()
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
    }
}
