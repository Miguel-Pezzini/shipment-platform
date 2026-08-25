namespace ShipmentPlatform.Infrastructure.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public DemoUserOptions DemoUser { get; set; } = new();
}

public sealed class DemoUserOptions
{
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "Admin123!";
}
