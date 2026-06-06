namespace MediTrack.ReminderService.Domain.Enums;

/// <summary>
/// Canal por el que se intenta entregar la notificación. Push es el canal
/// primario (FCM, CON-05); Sms y Local son canales de fallback (AC-09).
/// </summary>
public enum NotificationChannel
{
    Push = 0,
    Sms = 1,
    Local = 2
}
