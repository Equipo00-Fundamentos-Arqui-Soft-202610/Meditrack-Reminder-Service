using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediTrack.ReminderService.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_message");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");

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
