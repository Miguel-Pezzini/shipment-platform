using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ShipmentPlatform.Application.Abstractions;
using ShipmentPlatform.Infrastructure.Auth;
using ShipmentPlatform.Infrastructure.Caching;
using ShipmentPlatform.Infrastructure.Messaging;
using ShipmentPlatform.Infrastructure.Options;
using ShipmentPlatform.Infrastructure.Persistence;
using ShipmentPlatform.Infrastructure.Persistence.Repositories;

namespace ShipmentPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        services.AddHostedService<OutboxProcessorService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        AddCache(services, configuration);
        AddMessaging(services, configuration);
        AddJwtAuthentication(services, configuration);

        return services;
    }

    private static void AddCache(IServiceCollection services, IConfiguration configuration)
    {
        if (configuration.GetValue("Redis:UseInMemory", false)
            || string.IsNullOrWhiteSpace(configuration["Redis:ConnectionString"]))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["Redis:ConnectionString"];
                options.InstanceName = "shipment-platform:";
            });
        }

        services.AddSingleton<ICache, JsonDistributedCache>();
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        var useInMemory = configuration.GetValue("Messaging:UseInMemory", false);
        var rabbit = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<ShipmentCreatedConsumer>();
            x.AddConsumer<ShipmentStatusChangedConsumer>();

            if (useInMemory)
            {
                x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            }
            else
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbit.Host, rabbit.Port, "/", h =>
                    {
                        h.Username(rabbit.Username);
                        h.Password(rabbit.Password);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            }
        });
    }

    private static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration is required.");

        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();
    }
}
