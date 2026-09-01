using FluentAssertions;
using Moq;
using ShipmentPlatform.Application.Abstractions;
using ShipmentPlatform.Application.DTOs;
using ShipmentPlatform.Application.Events;
using ShipmentPlatform.Application.Services;
using ShipmentPlatform.Domain.Entities;
using ShipmentPlatform.Domain.Exceptions;
using ShipmentPlatform.UnitTests.Fakes;

namespace ShipmentPlatform.UnitTests.Application;

public class ShipmentServiceTests
{
    private readonly Mock<IShipmentRepository> _repository = new();
    private readonly Mock<IShipmentTimelineRepository> _timeline = new();
    private readonly Mock<IEventPublisher> _publisher = new();
    private readonly ICache _cache = new InMemoryCache();
    private readonly ShipmentService _sut;

    public ShipmentServiceTests()
    {
        _sut = new ShipmentService(
            _repository.Object,
            _timeline.Object,
            _publisher.Object,
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

    [Fact]
    public async Task GetTimelineByIdAsync_WhenMissing_ShouldReturnNull()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shipment?)null);

        var result = await _sut.GetTimelineByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
        _timeline.Verify(
            t => t.ListByShipmentIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTimelineByIdAsync_WhenExists_ShouldReturnEntries()
    {
        var shipment = Shipment.Create("ACME", "Cliente", "Curitiba", "São Paulo", 8);
        var entries = new List<ShipmentTimelineEntryResponse>
        {
            new(
                Guid.NewGuid(),
                shipment.Id,
                shipment.TrackingCode,
                "Created",
                "Shipment created: Curitiba → São Paulo",
                null,
                null,
                DateTime.UtcNow)
        };

        _repository
            .Setup(r => r.GetByIdAsync(shipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shipment);

        _timeline
            .Setup(t => t.ListByShipmentIdAsync(shipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var result = await _sut.GetTimelineByIdAsync(shipment.Id);

        result.Should().BeEquivalentTo(entries);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPagedResult()
    {
        var newer = Shipment.Create("ACME", "Cliente", "Curitiba", "São Paulo", 8);
        var older = Shipment.Create("Beta", "Cliente", "Joinville", "Blumenau", 3);
        var query = new PagedQuery { Page = 1, PerPage = 2 };

        _repository
            .Setup(r => r.ListPagedAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([newer, older], 5));

        var result = await _sut.GetAllAsync(query);

        result.Items.Should().HaveCount(2);
        result.Items[0].SenderName.Should().Be("ACME");
        result.Page.Should().Be(1);
        result.PerPage.Should().Be(2);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
    }
}
