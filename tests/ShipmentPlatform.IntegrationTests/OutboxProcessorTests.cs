using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShipmentPlatform.Application.DTOs;
using ShipmentPlatform.Infrastructure.Persistence;
using ShipmentPlatform.Infrastructure.Persistence.Entities;

namespace ShipmentPlatform.IntegrationTests;

[Collection("Integration")]
public class OutboxProcessorTests
{
    private readonly CustomWebApplicationFactory _factory;

    public OutboxProcessorTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateShipment_ShouldMarkOutboxEventProcessed()
    {
        var client = await CreateAuthenticatedClientAsync();
        var request = new CreateShipmentRequest("Transportadora X", "Cliente Y", "Joinville", "Blumenau", 15.2m);

        var createResponse = await client.PostAsJsonAsync("/api/shipments", request);
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<ShipmentResponse>();
        created.Should().NotBeNull();

        var processed = await WaitForOutboxAsync(
            created!.Id,
            entry => entry.ProcessedAtUtc is not null,
            TimeSpan.FromSeconds(15));

        processed.ProcessedAtUtc.Should().NotBeNull();
        processed.PoisonedAtUtc.Should().BeNull();

        var claimed = await WaitUntilAsync(async () =>
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await db.InboxMessages.AnyAsync(x =>
                x.MessageId == processed.Id && x.ConsumerName == "ShipmentCreatedConsumer");
        }, TimeSpan.FromSeconds(10));

        claimed.Should().BeTrue();
    }

    [Fact]
    public async Task UnknownEventType_ShouldPoisonAndNotMarkProcessed()
    {
        var outboxEventId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.OutboxEvents.Add(new OutboxEvent
            {
                Id = outboxEventId,
                EventType = "Unknown.Event.Type",
                Payload = "{}",
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var poisoned = await WaitForOutboxAsync(
            outboxEventId,
            entry => entry.PoisonedAtUtc is not null,
            TimeSpan.FromSeconds(15),
            matchByEventId: true);

        poisoned.PoisonedAtUtc.Should().NotBeNull();
        poisoned.ProcessedAtUtc.Should().BeNull();
        poisoned.LastError.Should().Contain("Unknown outbox event type");
    }

    private async Task<OutboxEvent> WaitForOutboxAsync(
        Guid id,
        Func<OutboxEvent, bool> predicate,
        TimeSpan timeout,
        bool matchByEventId = false)
    {
        var deadline = DateTime.UtcNow + timeout;
        OutboxEvent? last = null;

        while (DateTime.UtcNow < deadline)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            last = matchByEventId
                ? await db.OutboxEvents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
                : await db.OutboxEvents.AsNoTracking().FirstOrDefaultAsync(x => x.Payload.Contains(id.ToString()));

            if (last is not null && predicate(last))
                return last;

            await Task.Delay(200);
        }

        throw new TimeoutException(
            $"Outbox event {id} did not reach the expected state. Last error: {last?.LastError}");
    }

    private static async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return true;

            await Task.Delay(200);
        }

        return false;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "admin", password = "Admin123!" });

        loginResponse.EnsureSuccessStatusCode();
        var payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        payload.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.AccessToken);
        return client;
    }

    private sealed record LoginResponseDto(string AccessToken, DateTime ExpiresAtUtc);
}
