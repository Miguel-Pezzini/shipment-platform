namespace ShipmentPlatform.Infrastructure.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public string Username { get; set; } = "shipment";
    public string Password { get; set; } = "shipment";
    public ushort Port { get; set; } = 5672;
}
