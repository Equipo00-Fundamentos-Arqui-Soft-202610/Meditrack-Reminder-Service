using MediTrack.ReminderService.Domain.Enums;

namespace MediTrack.ReminderService.Application.IntegrationEvents;

/// <summary>
/// Publicado por el Follow-up Service al registrar un cumplimiento (US06). El
/// Reminder Service lo consume y cancela el recordatorio pendiente asociado a la
/// entidad cumplida (patrón Observer, 4.1.6).
/// </summary>
public sealed record CumplimientoRegistradoEvent : IntegrationEvent
{
    public override string EventType => "CumplimientoRegistrado";

    public int PatientId { get; init; }

    public ReminderEntityType EntityType { get; init; }

    public long EntityId { get; init; }
}
