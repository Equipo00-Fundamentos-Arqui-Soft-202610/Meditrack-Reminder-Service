namespace MediTrack.ReminderService.Domain.Enums;

/// <summary>
/// Estados del ciclo de vida de un recordatorio.
/// </summary>
public enum ReminderStatus
{
    /// <summary>Programado y pendiente de envío.</summary>
    Scheduled = 0,

    /// <summary>Entregado correctamente al menos por un canal.</summary>
    Sent = 1,

    /// <summary>Cancelado (p. ej. tras CumplimientoRegistrado).</summary>
    Cancelled = 2,

    /// <summary>Agotados los reintentos sin entrega confirmada.</summary>
    Failed = 3
}
