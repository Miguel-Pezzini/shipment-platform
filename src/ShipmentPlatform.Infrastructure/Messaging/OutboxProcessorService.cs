using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShipmentPlatform.Infrastructure.Options;
using ShipmentPlatform.Infrastructure.Persistence;
using ShipmentPlatform.Infrastructure.Persistence.Entities;

namespace ShipmentPlatform.Infrastructure.Messaging;

public sealed class OutboxProcessorService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessorService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollingInterval = TimeSpan.FromSeconds(Math.Max(1, options.Value.PollingIntervalSeconds));

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

            await Task.Delay(pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingEventsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var outbox = options.Value;
        var now = DateTime.UtcNow;
        var batchSize = Math.Max(1, outbox.BatchSize);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var pending = await db.OutboxEvents
            .FromSql($"""
                SELECT * FROM outbox_events
                WHERE "Id" IN (
                    SELECT "Id" FROM outbox_events
                    WHERE "ProcessedAtUtc" IS NULL
                      AND "PoisonedAtUtc" IS NULL
                      AND ("NextAttemptAtUtc" IS NULL OR "NextAttemptAtUtc" <= {now})
                    ORDER BY "CreatedAtUtc"
                    LIMIT {batchSize}
                    FOR UPDATE SKIP LOCKED
                )
                """)
            .ToListAsync(cancellationToken);

        foreach (var entry in pending)
            await ProcessEntryAsync(entry, publishEndpoint, outbox.MaxAttempts, now, cancellationToken);

        if (pending.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task ProcessEntryAsync(
        OutboxEvent entry,
        IPublishEndpoint publishEndpoint,
        int maxAttempts,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (!OutboxEventTypeMap.TryGetType(entry.EventType, out var eventType))
        {
            Poison(entry, nowUtc, $"Unknown outbox event type: {entry.EventType}");
            logger.LogError("Unknown outbox event type {EventType} for {OutboxEventId}", entry.EventType, entry.Id);
            return;
        }

        object? message;
        try
        {
            message = JsonSerializer.Deserialize(entry.Payload, eventType, JsonOptions);
        }
        catch (JsonException ex)
        {
            Poison(entry, nowUtc, Truncate($"Failed to deserialize: {ex.Message}"));
            logger.LogError(ex, "Failed to deserialize outbox event {OutboxEventId}", entry.Id);
            return;
        }

        if (message is null)
        {
            Poison(entry, nowUtc, "Failed to deserialize outbox event payload.");
            logger.LogError("Failed to deserialize outbox event {OutboxEventId}", entry.Id);
            return;
        }

        try
        {
            await PublishWithMessageIdAsync(publishEndpoint, message, eventType, entry.Id, cancellationToken);

            entry.ProcessedAtUtc = nowUtc;
            entry.LastError = null;
            entry.NextAttemptAtUtc = null;
            logger.LogInformation("Published outbox event {OutboxEventId} {EventType}", entry.Id, eventType.Name);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ScheduleRetryOrPoison(entry, nowUtc, maxAttempts, Truncate(ex.Message));
            logger.LogError(
                ex,
                "Failed to publish outbox event {OutboxEventId}; attempt {Attempt}/{MaxAttempts}",
                entry.Id,
                entry.AttemptCount,
                maxAttempts);
        }
    }

    private static Task PublishWithMessageIdAsync(
        IPublishEndpoint publishEndpoint,
        object message,
        Type eventType,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var typedPublish = typeof(OutboxProcessorService)
            .GetMethod(nameof(PublishTypedAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(eventType);

        return (Task)typedPublish.Invoke(null, [publishEndpoint, message, messageId, cancellationToken])!;
    }

    private static Task PublishTypedAsync<T>(
        IPublishEndpoint publishEndpoint,
        object message,
        Guid messageId,
        CancellationToken cancellationToken)
        where T : class =>
        publishEndpoint.Publish(
            (T)message,
            ctx => ctx.MessageId = messageId,
            cancellationToken);

    private static void ScheduleRetryOrPoison(OutboxEvent entry, DateTime nowUtc, int maxAttempts, string error)
    {
        entry.AttemptCount++;
        entry.LastError = error;

        if (OutboxRetryPolicy.IsPoison(entry.AttemptCount, maxAttempts))
        {
            entry.PoisonedAtUtc = nowUtc;
            entry.NextAttemptAtUtc = null;
            return;
        }

        entry.NextAttemptAtUtc = OutboxRetryPolicy.NextAttemptAt(nowUtc, entry.AttemptCount);
    }

    private static void Poison(OutboxEvent entry, DateTime nowUtc, string error)
    {
        entry.AttemptCount++;
        entry.LastError = error;
        entry.PoisonedAtUtc = nowUtc;
        entry.NextAttemptAtUtc = null;
    }

    private static string Truncate(string value, int maxLength = 2000) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
