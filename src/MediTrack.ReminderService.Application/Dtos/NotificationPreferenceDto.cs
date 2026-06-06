using MediTrack.ReminderService.Domain.Entities;

namespace MediTrack.ReminderService.Application.Dtos;

/// <summary>Vista de lectura de las preferencias de notificación de un paciente.</summary>
public sealed record NotificationPreferenceDto(
    long PatientId,
    bool SoundEnabled,
    bool VibrationEnabled,
    int RepeatCount,
    bool GlobalEnabled)
{
    public static NotificationPreferenceDto FromEntity(NotificationPreference preference) => new(
        preference.PatientId,
        preference.SoundEnabled,
        preference.VibrationEnabled,
        preference.RepeatCount,
        preference.GlobalEnabled);
}

/// <summary>Cuerpo de la petición para actualizar las preferencias (US22).</summary>
public sealed record UpdateNotificationPreferenceRequest(
    bool SoundEnabled,
    bool VibrationEnabled,
    int RepeatCount,
    bool GlobalEnabled);
