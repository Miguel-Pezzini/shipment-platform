namespace ShipmentPlatform.Infrastructure.Messaging;

public static class OutboxRetryPolicy
{
    public static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(5);

    public static bool IsPoison(int attemptCount, int maxAttempts) =>
        attemptCount >= maxAttempts;

    public static DateTime NextAttemptAt(DateTime nowUtc, int attemptCount)
    {
        var exponent = Math.Max(attemptCount - 1, 0);
        return nowUtc + BaseDelay * Math.Pow(2, exponent);
    }
}
