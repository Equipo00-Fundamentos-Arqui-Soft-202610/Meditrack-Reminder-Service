using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediTrack.ReminderService.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_message");

        builder.HasKey(m => m.Id);

        // Se almacena como binary(16) en lugar del char(36) implícito de Pomelo:
        // evita la collation ascii_general_ci, rechazada por TiDB Cloud
        // ("new collation framework"), y es más compacto/rápido para índices.
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .HasConversion(id => id.ToByteArray(), bytes => new Guid(bytes))
            .HasColumnType("binary(16)");

        builder.Property(m => m.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        builder.Property(m => m.Payload).HasColumnName("payload").HasColumnType("json").IsRequired();
        builder.Property(m => m.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        builder.Property(m => m.OccurredAtUtc).HasColumnName("occurred_at").IsRequired();
        builder.Property(m => m.ProcessedAtUtc).HasColumnName("processed_at");
        builder.Property(m => m.Attempts).HasColumnName("attempts").IsRequired();
        builder.Property(m => m.LastError).HasColumnName("last_error").HasMaxLength(500);

        builder.HasIndex(m => m.ProcessedAtUtc).HasDatabaseName("ix_outbox_processed_at");
    }
}
