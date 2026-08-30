using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShipmentPlatform.Infrastructure.Persistence.Entities;

namespace ShipmentPlatform.Infrastructure.Persistence.Configurations;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        builder.HasKey(x => new { x.MessageId, x.ConsumerName });

        builder.Property(x => x.ConsumerName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ProcessedAtUtc)
            .IsRequired();
    }
}
