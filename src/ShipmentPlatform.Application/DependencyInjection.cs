using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ShipmentPlatform.Application.Services;

namespace ShipmentPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ShipmentService>();
        services.AddScoped<IShipmentService, ShipmentService>();
        return services;
    }
}
