using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ShipmentPlatform.Application.DTOs;
using ShipmentPlatform.Application.Timeline;

namespace ShipmentPlatform.IntegrationTests;

[Collection("Integration")]
public class ShipmentTimelineTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly CustomWebApplicationFactory _factory;

    public ShipmentTimelineTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateAndUpdate_ShouldProjectTimelineInOrder()
    {
        var client = await CreateAuthenticatedClientAsync();
        var created = await CreateShipmentAsync(client);

        var createdEntry = await WaitForTimelineEntryAsync(
            client,
            created.Id,
            entry => entry.EventType == ShipmentTimeline.Created);

        createdEntry.Description.Should().Contain(created.OriginCity);
        createdEntry.Description.Should().Contain(created.DestinationCity);

        var patch = await client.PatchAsJsonAsync(
            $"/api/shipments/{created.Id}/status",
            new UpdateShipmentStatusRequest("PickedUp"));
        patch.EnsureSuccessStatusCode();

        await WaitForTimelineEntryAsync(
            client,
            created.Id,
            entry => entry.EventType == ShipmentTimeline.StatusChanged
                     && entry.NewStatus == "PickedUp");

        var timeline = await client.GetFromJsonAsync<List<ShipmentTimelineEntryResponse>>(
            $"/api/shipments/{created.Id}/timeline",
            JsonOptions);

        timeline.Should().HaveCount(2);
        timeline![0].EventType.Should().Be(ShipmentTimeline.Created);
        timeline[1].EventType.Should().Be(ShipmentTimeline.StatusChanged);
        timeline[1].PreviousStatus.Should().Be("Created");
        timeline[1].NewStatus.Should().Be("PickedUp");

        var anonymous = _factory.CreateClient();
        var publicTimeline = await anonymous.GetFromJsonAsync<List<ShipmentTimelineEntryResponse>>(
            $"/api/shipments/tracking/{created.TrackingCode}/timeline",
            JsonOptions);

        publicTimeline.Should().BeEquivalentTo(timeline);
    }

    [Fact]
    public async Task GetTimeline_WhenShipmentMissing_ShouldReturnNotFound()
    {
        var client = await CreateAuthenticatedClientAsync();
        var response = await client.GetAsync($"/api/shipments/{Guid.NewGuid()}/timeline");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<ShipmentTimelineEntryResponse> WaitForTimelineEntryAsync(
        HttpClient client,
        Guid shipmentId,
        Func<ShipmentTimelineEntryResponse, bool> predicate)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);

        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/api/shipments/{shipmentId}/timeline");
            if (response.IsSuccessStatusCode)
            {
                var timeline = await response.Content.ReadFromJsonAsync<List<ShipmentTimelineEntryResponse>>(JsonOptions);
                var match = timeline?.FirstOrDefault(predicate);
                if (match is not null)
                    return match;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"Timeline for {shipmentId} did not reach the expected state.");
    }

    private async Task<ShipmentResponse> CreateShipmentAsync(HttpClient client)
    {
        var request = new CreateShipmentRequest("Transportadora X", "Cliente Y", "Joinville", "Blumenau", 10);
        var response = await client.PostAsJsonAsync("/api/shipments", request);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<ShipmentResponse>(JsonOptions);
        created.Should().NotBeNull();
        return created!;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "admin", password = "Admin123!" });

        loginResponse.EnsureSuccessStatusCode();
        var payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        payload.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.AccessToken);
        return client;
    }

    private sealed record LoginResponseDto(string AccessToken, DateTime ExpiresAtUtc);
}
