using MediTrack.ReminderService.Domain.Enums;

namespace MediTrack.ReminderService.Application.IntegrationEvents;

/// <summary>
/// Publicado por el Reminder Service tras entregar (o fallar) un recordatorio.
/// Permite que el Medical Analysis Service alimente sus métricas de adherencia.
/// Se emite mediante el patrón Outbox para garantizar entrega confiable (AC-08).
/// </summary>
public sealed record RecordatorioEnviadoEvent : IntegrationEvent
{
    public override string EventType => "RecordatorioEnviado";

    public long ReminderId { get; init; }

    public int PatientId { get; init; }

    public ReminderEntityType EntityType { get; init; }

    public long EntityId { get; init; }

    public NotificationChannel Channel { get; init; }

    public bool Delivered { get; init; }

    public DateTime SentAtUtc { get; init; }
}
