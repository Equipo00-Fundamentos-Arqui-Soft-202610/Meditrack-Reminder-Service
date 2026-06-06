namespace MediTrack.ReminderService.Application.IntegrationEvents;

/// <summary>
/// Publicado por el Medical Appointment Service al agendar una cita (US08). El
/// Reminder Service genera un recordatorio con 24 h de anticipación (US09).
/// </summary>
public sealed record CitaAgendadaEvent : IntegrationEvent
{
    public override string EventType => "CitaAgendada";

    public long PatientId { get; init; }

    public long AppointmentId { get; init; }

    public string AppointmentType { get; init; } = string.Empty;

    public string? Location { get; init; }

    public DateTime AppointmentDateUtc { get; init; }
}
