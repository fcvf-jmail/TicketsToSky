using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketsToSky.Api.Models;

namespace TicketsToSky.Api.Configuration;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.MaxPrice)
            .IsRequired()
            .HasColumnType("integer");

        builder.Property(s => s.DepartureAirport)
            .IsRequired()
            .HasColumnType("varchar(10)");

        builder.Property(s => s.ArrivalAirport)
            .IsRequired()
            .HasColumnType("varchar(10)");

        builder.Property(s => s.DepartureDate)
            .IsRequired();

        builder.Property(s => s.MaxTransfersCount)
            .IsRequired()
            .HasColumnType("integer");

        builder.Property(s => s.MinBaggageAmount)
            .IsRequired()
            .HasColumnType("integer");

        builder.Property(s => s.MinHandbagsAmount)
            .IsRequired()
            .HasColumnType("integer");

        builder.Property(s => s.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(s => s.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(s => s.LastChecked)
            .IsRequired(false)
            .HasColumnType("timestamp with time zone");
    }
}