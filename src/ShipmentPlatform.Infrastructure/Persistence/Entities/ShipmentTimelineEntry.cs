namespace ShipmentPlatform.Infrastructure.Persistence.Entities;

public class ShipmentTimelineEntry
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid ShipmentId { get; set; }
    public required string TrackingCode { get; set; }
    public required string EventType { get; set; }
    public required string Description { get; set; }
    public string? PreviousStatus { get; set; }
    public string? NewStatus { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
