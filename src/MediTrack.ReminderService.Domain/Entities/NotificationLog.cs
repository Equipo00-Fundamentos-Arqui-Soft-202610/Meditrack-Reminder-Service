using MediTrack.ReminderService.Domain.Enums;

namespace MediTrack.ReminderService.Domain.Entities;

/// <summary>
/// Bitácora de cada intento de entrega de notificación. Permite auditar el
/// cumplimiento del QAS-2 (99.9 % de recordatorios críticos entregados) y el uso
/// de canales de fallback. Mapea a la tabla <c>notification_log</c>.
/// </summary>
public class NotificationLog
{
    private NotificationLog() { }

    public long Id { get; private set; }

    public long ReminderId { get; private set; }

    public DateTime SentAt { get; private set; }

    public NotificationChannel Channel { get; private set; }

    public DeliveryStatus DeliveryStatus { get; private set; }

    internal static NotificationLog Create(
        NotificationChannel channel,
        DeliveryStatus deliveryStatus,
        DateTime sentAtUtc) => new()
        {
            Channel = channel,
            DeliveryStatus = deliveryStatus,
            SentAt = sentAtUtc
        };
}
