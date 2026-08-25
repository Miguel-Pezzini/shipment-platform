using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ShipmentPlatform.Application.DTOs;

namespace ShipmentPlatform.IntegrationTests;

[Collection("Integration")]
public class ShipmentsApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly CustomWebApplicationFactory _factory;

    public ShipmentsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateAndGetShipment_ShouldWorkEndToEnd()
    {
        var client = await CreateAuthenticatedClientAsync();

        var request = new CreateShipmentRequest(
            "Transportadora X",
            "Cliente Y",
            "Joinville",
            "Blumenau",
            15.2m);

        var createResponse = await client.PostAsJsonAsync("/api/shipments", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ShipmentResponse>(JsonOptions);
        created.Should().NotBeNull();
        created!.TrackingCode.Should().NotBeNullOrWhiteSpace();

        var getResponse = await client.GetAsync($"/api/shipments/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ShipmentResponse>(JsonOptions);
        fetched!.TrackingCode.Should().Be(created.TrackingCode);
        fetched.Status.Should().Be("Created");

        var anonymous = _factory.CreateClient();
        var trackingResponse = await anonymous.GetAsync($"/api/shipments/tracking/{created.TrackingCode}");
        trackingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new CreateShipmentRequest("A", "B", "C", "D", 1);
        var response = await client.PostAsJsonAsync("/api/shipments", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        var client = await CreateAuthenticatedClientAsync();
        var response = await client.GetAsync($"/api/shipments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "admin", password = "Admin123!" });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.AccessToken.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", payload.AccessToken);
        return client;
    }

    private sealed record LoginResponseDto(string AccessToken, DateTime ExpiresAtUtc);
}

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<CustomWebApplicationFactory>;
