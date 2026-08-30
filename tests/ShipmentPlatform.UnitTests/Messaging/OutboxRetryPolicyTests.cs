using FluentAssertions;
using ShipmentPlatform.Infrastructure.Messaging;

namespace ShipmentPlatform.UnitTests.Messaging;

public class OutboxRetryPolicyTests
{
    private static readonly DateTime Now = new(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NextAttemptAt_FirstFailure_UsesBaseDelay()
    {
        OutboxRetryPolicy.NextAttemptAt(Now, 1).Should().Be(Now.AddSeconds(5));
    }

    [Fact]
    public void NextAttemptAt_SecondFailure_DoublesDelay()
    {
        OutboxRetryPolicy.NextAttemptAt(Now, 2).Should().Be(Now.AddSeconds(10));
    }

    [Fact]
    public void NextAttemptAt_FourthFailure_UsesExponentialBackoff()
    {
        OutboxRetryPolicy.NextAttemptAt(Now, 4).Should().Be(Now.AddSeconds(40));
    }

    [Theory]
    [InlineData(4, 5, false)]
    [InlineData(5, 5, true)]
    [InlineData(6, 5, true)]
    public void IsPoison_RespectsMaxAttempts(int attemptCount, int maxAttempts, bool expected)
    {
        OutboxRetryPolicy.IsPoison(attemptCount, maxAttempts).Should().Be(expected);
    }
}
