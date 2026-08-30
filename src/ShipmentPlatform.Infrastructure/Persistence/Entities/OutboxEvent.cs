namespace ShipmentPlatform.Infrastructure.Persistence.Entities;

public class OutboxEvent
{
    public Guid Id { get; set; }
    public required string EventType { get; set; }
    public required string Payload { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public DateTime? PoisonedAtUtc { get; set; }
}
