namespace ShipmentPlatform.Infrastructure.Persistence.Entities;

public class InboxMessage
{
    public Guid MessageId { get; set; }
    public required string ConsumerName { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
}
