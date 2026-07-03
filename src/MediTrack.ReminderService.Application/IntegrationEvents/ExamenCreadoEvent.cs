using MediTrack.ReminderService.Application.IntegrationEvents;

namespace MediTrack.ReminderService.Application.IntegrationEvents;

public sealed record ExamenCreadoEvent : IntegrationEvent
{
    public override string EventType => "ExamenCreado";

    public int PatientId { get; init; }

    public long ExamId { get; init; }

    public string ExamType { get; init; } = string.Empty;

    public DateTime? PickupDate { get; init; }
}
