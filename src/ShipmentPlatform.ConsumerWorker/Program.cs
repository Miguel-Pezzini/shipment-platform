using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ShipmentPlatform.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddConsumerWorkerInfrastructure(builder.Configuration);

var metricsHost = builder.Configuration["Metrics:Host"] ?? "0.0.0.0";
var metricsPort = builder.Configuration.GetValue("Metrics:Port", 9465);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "ShipmentPlatform.ConsumerWorker",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
    .WithTracing(tracing => tracing
        .AddHttpClientInstrumentation()
        .AddSource("MassTransit"))
    .WithMetrics(metrics => metrics
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("MassTransit")
        .AddPrometheusHttpListener(options =>
        {
            options.Host = metricsHost;
            options.Port = metricsPort;
        }));

await builder.Build().RunAsync();
