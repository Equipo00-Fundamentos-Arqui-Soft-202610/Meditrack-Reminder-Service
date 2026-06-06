using MediTrack.ReminderService.Domain.Entities;
using MediTrack.ReminderService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediTrack.ReminderService.Infrastructure.Persistence.Configurations;

internal sealed class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable("reminder");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.PatientId).HasColumnName("patient_id").IsRequired();

        builder.Property(r => r.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(30)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.EntityId).HasColumnName("entity_id").IsRequired();

        builder.Property(r => r.ScheduledAt).HasColumnName("scheduled_at").IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.CancelledAt).HasColumnName("cancelled_at");

        builder.Property(r => r.Title).HasColumnName("title").HasMaxLength(150).IsRequired();
        builder.Property(r => r.Body).HasColumnName("body").HasMaxLength(500).IsRequired();

        // Índices para las consultas críticas del Scheduler y de la API (táctica 4.1.7).
        builder.HasIndex(r => new { r.PatientId, r.Status }).HasDatabaseName("ix_reminder_patient_status");
        builder.HasIndex(r => new { r.Status, r.ScheduledAt }).HasDatabaseName("ix_reminder_status_scheduled");
        builder.HasIndex(r => new { r.PatientId, r.EntityType, r.EntityId })
            .HasDatabaseName("ix_reminder_patient_entity");

        builder.HasMany(r => r.NotificationLogs)
            .WithOne()
            .HasForeignKey(l => l.ReminderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Reminder.NotificationLogs))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
