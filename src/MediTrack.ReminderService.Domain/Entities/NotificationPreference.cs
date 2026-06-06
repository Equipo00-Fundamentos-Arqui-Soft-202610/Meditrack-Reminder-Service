namespace MediTrack.ReminderService.Domain.Entities;

/// <summary>
/// Preferencias de notificación por paciente (US22). El Reminder Service las
/// consulta antes de generar cada notificación para respetar sonido, vibración,
/// número de repeticiones y la desactivación temporal global. Mapea a
/// <c>notification_preference</c>.
/// </summary>
public class NotificationPreference
{
    private NotificationPreference() { }

    public long Id { get; private set; }

    public long PatientId { get; private set; }

    public bool SoundEnabled { get; private set; }

    public bool VibrationEnabled { get; private set; }

    /// <summary>Cantidad de repeticiones de la notificación (mín. 1).</summary>
    public int RepeatCount { get; private set; }

    /// <summary>Interruptor maestro: si está en false, no se envía ningún recordatorio.</summary>
    public bool GlobalEnabled { get; private set; }

    /// <summary>Preferencia por defecto para un paciente sin configuración explícita.</summary>
    public static NotificationPreference Default(long patientId) => new()
    {
        PatientId = patientId,
        SoundEnabled = true,
        VibrationEnabled = true,
        RepeatCount = 1,
        GlobalEnabled = true
    };

    public void Update(bool soundEnabled, bool vibrationEnabled, int repeatCount, bool globalEnabled)
    {
        if (repeatCount < 1)
            throw new ArgumentOutOfRangeException(nameof(repeatCount), "Las repeticiones deben ser al menos 1.");

        SoundEnabled = soundEnabled;
        VibrationEnabled = vibrationEnabled;
        RepeatCount = repeatCount;
        GlobalEnabled = globalEnabled;
    }
}
