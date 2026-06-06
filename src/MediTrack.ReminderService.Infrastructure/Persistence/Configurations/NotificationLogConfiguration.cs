using MediTrack.ReminderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediTrack.ReminderService.Infrastructure.Persistence.Configurations;

internal sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("notification_log");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");

        builder.Property(l => l.ReminderId).HasColumnName("reminder_id").IsRequired();
        builder.Property(l => l.SentAt).HasColumnName("sent_at").IsRequired();

        builder.Property(l => l.Channel)
            .HasColumnName("channel")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(l => l.DeliveryStatus)
            .HasColumnName("delivery_status")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.HasIndex(l => l.ReminderId).HasDatabaseName("ix_notification_log_reminder");
    }
}
