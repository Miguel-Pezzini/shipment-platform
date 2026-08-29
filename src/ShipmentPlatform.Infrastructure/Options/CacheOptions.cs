namespace ShipmentPlatform.Infrastructure.Options;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public int DefaultExpirationMinutes { get; set; } = 5;
}
