using MediTrack.ReminderService.Domain.Enums;

namespace MediTrack.ReminderService.Domain.Entities;

/// <summary>
/// Raíz de agregado del Reminder Service. Representa un recordatorio programado
/// para un paciente, derivado de un evento clínico (RecetaCargada, CitaAgendada,
/// etc.). Encapsula su ciclo de vida: programación, marca de envío y cancelación.
///
/// Las invariantes viven dentro de la entidad (modelo de dominio rico): el resto
/// del sistema no muta su estado directamente, sino a través de los métodos de
/// comportamiento. Ver "Reminder Domain Model" en el diagrama de componentes (Fig. 17).
/// </summary>
public class Reminder
{
    private readonly List<NotificationLog> _notificationLogs = new();

    // Constructor privado: la creación se realiza vía Factory Method (ver Factories/).
    private Reminder() { }

    public long Id { get; private set; }

    public long PatientId { get; private set; }

    /// <summary>Tipo de entidad que originó el recordatorio (medication/appointment/exam).</summary>
    public ReminderEntityType EntityType { get; private set; }

    /// <summary>Identificador de la entidad origen en su microservicio (FK lógica, no física).</summary>
    public long EntityId { get; private set; }

    /// <summary>Instante en que el recordatorio debe dispararse.</summary>
    public DateTime ScheduledAt { get; private set; }

    public ReminderStatus Status { get; private set; }

    public DateTime? CancelledAt { get; private set; }

    /// <summary>Título de la notificación, construido por la fábrica especializada.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Cuerpo de la notificación, construido por la fábrica especializada.</summary>
    public string Body { get; private set; } = string.Empty;

    public IReadOnlyCollection<NotificationLog> NotificationLogs => _notificationLogs.AsReadOnly();

    /// <summary>
    /// Fábrica interna usada exclusivamente por las clases Factory del dominio.
    /// Mantiene el constructor cerrado y centraliza la validación de invariantes.
    /// </summary>
    internal static Reminder Schedule(
        long patientId,
        ReminderEntityType entityType,
        long entityId,
        DateTime scheduledAt,
        string title,
        string body)
    {
        if (patientId <= 0)
            throw new ArgumentException("El paciente del recordatorio es obligatorio.", nameof(patientId));
        if (entityId <= 0)
            throw new ArgumentException("La entidad origen del recordatorio es obligatoria.", nameof(entityId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El recordatorio requiere un título.", nameof(title));

        return new Reminder
        {
            PatientId = patientId,
            EntityType = entityType,
            EntityId = entityId,
            ScheduledAt = scheduledAt,
            Title = title.Trim(),
            Body = (body ?? string.Empty).Trim(),
            Status = ReminderStatus.Scheduled
        };
    }

    /// <summary>
    /// Crea una alerta inmediata no derivada del flujo de fábricas programadas
    /// (p. ej. alerta de stock bajo, US07). El mensaje ya viene redactado por la
    /// capa de aplicación; se programa para envío inmediato.
    /// </summary>
    public static Reminder CreateAlert(
        long patientId,
        ReminderEntityType entityType,
        long entityId,
        DateTime whenUtc,
        string title,
        string body) => Schedule(patientId, entityType, entityId, whenUtc, title, body);

    /// <summary>Indica si el recordatorio ya venció y sigue pendiente de envío.</summary>
    public bool IsDue(DateTime asOfUtc) =>
        Status == ReminderStatus.Scheduled && ScheduledAt <= asOfUtc;

    /// <summary>
    /// Registra un intento de entrega. El recordatorio pasa a Sent si algún canal
    /// confirma la entrega; permanece Scheduled mientras haya reintentos pendientes.
    /// </summary>
    public NotificationLog RegisterDeliveryAttempt(
        NotificationChannel channel,
        DeliveryStatus deliveryStatus,
        DateTime attemptedAtUtc)
    {
        var log = NotificationLog.Create(channel, deliveryStatus, attemptedAtUtc);
        _notificationLogs.Add(log);

        if (deliveryStatus == DeliveryStatus.Delivered)
            Status = ReminderStatus.Sent;

        return log;
    }

    /// <summary>Marca el recordatorio como fallido tras agotar los reintentos.</summary>
    public void MarkAsFailed()
    {
        if (Status == ReminderStatus.Scheduled)
            Status = ReminderStatus.Failed;
    }

    /// <summary>
    /// Cancela el recordatorio. Operación idempotente: cancelar uno ya cancelado
    /// o ya enviado no produce error. Usado al recibir CumplimientoRegistrado (US06).
    /// </summary>
    public void Cancel(DateTime cancelledAtUtc)
    {
        if (Status is ReminderStatus.Cancelled or ReminderStatus.Sent)
            return;

        Status = ReminderStatus.Cancelled;
        CancelledAt = cancelledAtUtc;
    }
}
