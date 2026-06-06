namespace MediTrack.ReminderService.Domain.Enums;

/// <summary>
/// Resultado de un intento de entrega registrado en el NotificationLog.
/// </summary>
public enum DeliveryStatus
{
    Pending = 0,
    Delivered = 1,
    Failed = 2
}
