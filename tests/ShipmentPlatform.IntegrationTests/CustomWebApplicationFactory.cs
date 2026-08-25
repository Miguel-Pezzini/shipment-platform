using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShipmentPlatform.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ShipmentPlatform.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("shipment_platform_tests")
        .WithUsername("shipment")
        .WithPassword("shipment")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
        builder.UseSetting("Messaging:UseInMemory", "true");
        builder.UseSetting("Redis:UseInMemory", "true");
        builder.UseSetting("Jwt:Issuer", "ShipmentPlatform.Tests");
        builder.UseSetting("Jwt:Audience", "ShipmentPlatform.Tests");
        builder.UseSetting("Jwt:Key", "ShipmentPlatform-Test-Signing-Key-32chars!");
        builder.UseSetting("Jwt:ExpirationMinutes", "60");
        builder.UseSetting("Auth:DemoUser:Username", "admin");
        builder.UseSetting("Auth:DemoUser:Password", "Admin123!");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }
}
