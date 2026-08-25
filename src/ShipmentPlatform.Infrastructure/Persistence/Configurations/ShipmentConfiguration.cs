using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShipmentPlatform.Domain.Entities;

namespace ShipmentPlatform.Infrastructure.Persistence.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TrackingCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(x => x.TrackingCode)
            .IsUnique();

        builder.Property(x => x.SenderName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RecipientName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OriginCity).HasMaxLength(120).IsRequired();
        builder.Property(x => x.DestinationCity).HasMaxLength(120).IsRequired();

        builder.Property(x => x.WeightKg)
            .HasPrecision(10, 3);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32);
    }
}
