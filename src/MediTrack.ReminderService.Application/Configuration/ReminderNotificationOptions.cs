namespace MediTrack.ReminderService.Application.Configuration;

/// <summary>
/// Parámetros del envío de notificaciones y la política de reintentos con backoff
/// exponencial (AC-02). Se enlazan desde appsettings (sección "ReminderNotification").
/// </summary>
public sealed class ReminderNotificationOptions
{
    public const string SectionName = "ReminderNotification";

    /// <summary>Número máximo de intentos por el canal push antes del fallback.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Retardo base del backoff exponencial, en segundos (delay = base * 2^intento).</summary>
    public double BaseDelaySeconds { get; set; } = 2;

    /// <summary>Prefijo del topic de FCM por paciente.</summary>
    public string FcmTopicPrefix { get; set; } = "patient_";

    /// <summary>Activa el canal de fallback local cuando se agotan los reintentos push (AC-09).</summary>
    public bool EnableLocalFallback { get; set; } = true;

    public TimeSpan BackoffFor(int attempt) =>
        TimeSpan.FromSeconds(BaseDelaySeconds * Math.Pow(2, attempt));
}
