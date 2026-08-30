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
    public static IServiceCollection AddApiInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddOutboxWriter();
        services.AddCache(configuration);
        services.AddJwtAuthentication(configuration);
        return services;
    }

    public static IServiceCollection AddOutboxWorkerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddOutboxProcessor();
        services.AddMassTransitPublisher(configuration);
        return services;
    }

    public static IServiceCollection AddConsumerWorkerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddInbox();
        services.AddMassTransitConsumers(configuration);
        return services;
    }

    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IShipmentTimelineRepository, ShipmentTimelineRepository>();
        return services;
    }

    public static IServiceCollection AddOutboxWriter(this IServiceCollection services)
    {
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        return services;
    }

    public static IServiceCollection AddOutboxProcessor(this IServiceCollection services)
    {
        services.AddOptions<OutboxOptions>().BindConfiguration(OutboxOptions.SectionName);
        services.AddHostedService<OutboxProcessorService>();
        return services;
    }

    public static IServiceCollection AddInbox(this IServiceCollection services)
    {
        services.AddScoped<InboxGuard>();
        return services;
    }

    public static IServiceCollection AddCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<CacheOptions>().BindConfiguration(CacheOptions.SectionName);

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
        return services;
    }

    public static IServiceCollection AddMassTransitPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ConfigureMassTransit(services, ReadBusSettings(configuration), withConsumers: false);
        return services;
    }

    public static IServiceCollection AddMassTransitConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ConfigureMassTransit(services, ReadBusSettings(configuration), withConsumers: true);
        return services;
    }

    public static IServiceCollection AddMassTransitConsumers(
        this IServiceCollection services,
        bool useInMemory)
    {
        ConfigureMassTransit(services, (useInMemory, new RabbitMqOptions()), withConsumers: true);
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionName);
        services.AddOptions<AuthOptions>().BindConfiguration(AuthOptions.SectionName);
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

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
        return services;
    }

    private static (bool UseInMemory, RabbitMqOptions Rabbit) ReadBusSettings(IConfiguration configuration)
    {
        var useInMemory = configuration.GetValue("Messaging:UseInMemory", false);
        var rabbit = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();
        return (useInMemory, rabbit);
    }

    private static void ConfigureMassTransit(
        IServiceCollection services,
        (bool UseInMemory, RabbitMqOptions Rabbit) settings,
        bool withConsumers)
    {
        var (useInMemory, rabbit) = settings;

        services.AddMassTransit(x =>
        {
            if (withConsumers)
            {
                x.AddConsumer<ShipmentCreatedConsumer>();
                x.AddConsumer<ShipmentStatusChangedConsumer>();
            }

            if (useInMemory)
            {
                x.UsingInMemory((context, cfg) =>
                {
                    if (withConsumers)
                        cfg.ConfigureEndpoints(context);
                });
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

                    if (withConsumers)
                        cfg.ConfigureEndpoints(context);
                });
            }
        });
    }
}
