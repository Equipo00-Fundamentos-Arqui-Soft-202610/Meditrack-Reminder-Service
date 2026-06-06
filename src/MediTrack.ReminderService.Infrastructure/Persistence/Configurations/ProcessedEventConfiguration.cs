using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediTrack.ReminderService.Infrastructure.Persistence.Configurations;

internal sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("processed_event");

        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId).HasColumnName("event_id").ValueGeneratedNever();

        builder.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        builder.Property(e => e.ProcessedAtUtc).HasColumnName("processed_at").IsRequired();
    }
}
