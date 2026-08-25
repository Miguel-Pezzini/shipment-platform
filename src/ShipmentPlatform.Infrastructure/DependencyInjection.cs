using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShipmentPlatform.Application.Abstractions;
using ShipmentPlatform.Infrastructure.Messaging;
using ShipmentPlatform.Infrastructure.Persistence;
using ShipmentPlatform.Infrastructure.Persistence.Repositories;

namespace ShipmentPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddSingleton<IEventPublisher, LoggingEventPublisher>();

        return services;
    }
}
