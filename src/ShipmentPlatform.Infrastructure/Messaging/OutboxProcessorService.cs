using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShipmentPlatform.Infrastructure.Persistence;

namespace ShipmentPlatform.Infrastructure.Messaging;

public sealed class OutboxProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessorService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to process outbox events");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessPendingEventsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var pending = await db.OutboxEvents
            .Where(x => x.ProcessedAtUtc == null)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var entry in pending)
        {
            var eventType = Type.GetType(entry.EventType);
            logger.LogInformation("Processing outbox event {EventType}", eventType);
            if (eventType is null)
            {
                logger.LogError("Unknown outbox event type: {EventType}", entry.EventType);
                entry.ProcessedAtUtc = DateTime.UtcNow;
                continue;
            }

            var message = JsonSerializer.Deserialize(entry.Payload, eventType, JsonOptions);
            if (message is null)
            {
                logger.LogError("Failed to deserialize outbox event {OutboxEventId}", entry.Id);
                entry.ProcessedAtUtc = DateTime.UtcNow;
                continue;
            }

            await publishEndpoint.Publish(message, eventType, cancellationToken);
            entry.ProcessedAtUtc = DateTime.UtcNow;
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }
}
