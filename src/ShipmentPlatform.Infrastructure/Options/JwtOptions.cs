namespace ShipmentPlatform.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ShipmentPlatform";
    public string Audience { get; set; } = "ShipmentPlatform";
    public string Key { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
