using MediTrack.ReminderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediTrack.ReminderService.Infrastructure.Persistence.Configurations;

internal sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preference");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.PatientId).HasColumnName("patient_id").IsRequired();
        builder.Property(p => p.SoundEnabled).HasColumnName("sound_enabled").IsRequired();
        builder.Property(p => p.VibrationEnabled).HasColumnName("vibration_enabled").IsRequired();
        builder.Property(p => p.RepeatCount).HasColumnName("repeat_count").IsRequired();
        builder.Property(p => p.GlobalEnabled).HasColumnName("global_enabled").IsRequired();

        // Un único registro de preferencias por paciente.
        builder.HasIndex(p => p.PatientId).IsUnique().HasDatabaseName("ux_notification_preference_patient");
    }
}
