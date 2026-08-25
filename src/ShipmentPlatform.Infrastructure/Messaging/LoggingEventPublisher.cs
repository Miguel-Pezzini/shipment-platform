using System.Text.Json;
using Microsoft.Extensions.Logging;
using ShipmentPlatform.Application.Abstractions;

namespace ShipmentPlatform.Infrastructure.Messaging;

/// <summary>
/// Publisher inicial: registra o evento nos logs.
/// Troque por RabbitMQ/MassTransit sem mudar a Application.
/// </summary>
public class LoggingEventPublisher(ILogger<LoggingEventPublisher> logger) : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var payload = JsonSerializer.Serialize(@event, JsonOptions);
        logger.LogInformation("Event published: {EventType} {Payload}", typeof(TEvent).Name, payload);
        return Task.CompletedTask;
    }
}
