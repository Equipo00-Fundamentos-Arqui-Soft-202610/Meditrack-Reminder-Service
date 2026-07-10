namespace MediTrack.ReminderService.Domain.Enums;

/// <summary>
/// Resultado de un intento de entrega registrado en el NotificationLog.
/// </summary>
public enum DeliveryStatus
{
    Pending = 0,
    Delivered = 1,
    Failed = 2,

    /// <summary>
    /// Entregado al canal de respaldo local (AC-09): la app cliente es responsable
    /// de mostrar la notificación desde su almacenamiento offline. El backend no
    /// tiene visibilidad de si realmente se mostró, por lo que NO se marca como
    /// <see cref="Delivered"/> — requiere seguimiento manual/del cliente.
    /// </summary>
    RequiresManualFollowUp = 3
}
