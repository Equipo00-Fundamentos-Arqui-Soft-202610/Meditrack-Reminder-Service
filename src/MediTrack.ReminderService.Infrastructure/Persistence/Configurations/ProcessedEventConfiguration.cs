using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediTrack.ReminderService.Infrastructure.Persistence.Configurations;

internal sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("processed_event");

        builder.HasKey(e => e.EventId);

        // Se almacena como binary(16) en lugar del char(36) implícito de Pomelo:
        // evita la collation ascii_general_ci, rechazada por TiDB Cloud
        // ("new collation framework"), y es más compacto/rápido para índices.
        builder.Property(e => e.EventId)
            .HasColumnName("event_id")
            .HasConversion(id => id.ToByteArray(), bytes => new Guid(bytes))
            .HasColumnType("binary(16)")
            .ValueGeneratedNever();

        builder.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        builder.Property(e => e.ProcessedAtUtc).HasColumnName("processed_at").IsRequired();
    }
}
