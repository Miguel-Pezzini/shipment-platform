using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShipmentPlatform.Infrastructure.Persistence.Entities;

namespace ShipmentPlatform.Infrastructure.Persistence.Configurations;

public class ShipmentTimelineEntryConfiguration : IEntityTypeConfiguration<ShipmentTimelineEntry>
{
    public void Configure(EntityTypeBuilder<ShipmentTimelineEntry> builder)
    {
        builder.ToTable("shipment_timeline");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.MessageId).IsUnique();
        builder.HasIndex(x => x.ShipmentId);
        builder.HasIndex(x => x.TrackingCode);

        builder.Property(x => x.TrackingCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.PreviousStatus).HasMaxLength(32);
        builder.Property(x => x.NewStatus).HasMaxLength(32);
        builder.Property(x => x.OccurredAtUtc).IsRequired();
    }
}
