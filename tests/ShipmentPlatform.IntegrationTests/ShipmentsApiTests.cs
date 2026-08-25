using System.Net;
using System.Net.Http.Json;
using ShipmentPlatform.Application.DTOs;
using FluentAssertions;

namespace ShipmentPlatform.IntegrationTests;

public class ShipmentsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ShipmentsApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateAndGetShipment_ShouldWorkEndToEnd()
    {
        var request = new CreateShipmentRequest(
            "Transportadora X",
            "Cliente Y",
            "Joinville",
            "Blumenau",
            15.2m);

        var createResponse = await _client.PostAsJsonAsync("/api/shipments", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ShipmentResponse>();
        created.Should().NotBeNull();
        created!.TrackingCode.Should().NotBeNullOrWhiteSpace();

        var getResponse = await _client.GetAsync($"/api/shipments/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ShipmentResponse>();
        fetched!.TrackingCode.Should().Be(created.TrackingCode);
        fetched.Status.Should().Be("Created");
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync($"/api/shipments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
