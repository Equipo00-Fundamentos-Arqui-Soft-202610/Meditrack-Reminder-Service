namespace MediTrack.ReminderService.Application.Abstractions;

/// <summary>
/// Puerto de salida hacia el proveedor de notificaciones push. Lo implementa el
/// "Notification Adapter" sobre Firebase Cloud Messaging (CON-05). Devuelve el
/// resultado del intento para que la capa de aplicación decida reintentos y fallback.
/// </summary>
public interface INotificationSender
{
    Task<NotificationDeliveryResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}

/// <summary>Resultado de un intento de entrega.</summary>
/// <param name="Success">true si el proveedor aceptó la entrega.</param>
/// <param name="ProviderMessageId">Identificador devuelto por el proveedor (si lo hay).</param>
/// <param name="Error">Detalle del error cuando <paramref name="Success"/> es false.</param>
public sealed record NotificationDeliveryResult(bool Success, string? ProviderMessageId = null, string? Error = null)
{
    public static NotificationDeliveryResult Ok(string? providerMessageId = null) => new(true, providerMessageId);
    public static NotificationDeliveryResult Fail(string error) => new(false, null, error);
}
