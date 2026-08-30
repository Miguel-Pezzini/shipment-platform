using System.Text.Json;
using ShipmentPlatform.Application.Abstractions;
using ShipmentPlatform.Infrastructure.Persistence;
using ShipmentPlatform.Infrastructure.Persistence.Entities;

namespace ShipmentPlatform.Infrastructure.Messaging;

public sealed class OutboxEventPublisher(AppDbContext db) : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        db.OutboxEvents.Add(new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = OutboxEventTypeMap.GetTypeName(typeof(TEvent)),
            Payload = JsonSerializer.Serialize(@event, JsonOptions),
            CreatedAtUtc = DateTime.UtcNow
        });

        return Task.CompletedTask;
    }
}
