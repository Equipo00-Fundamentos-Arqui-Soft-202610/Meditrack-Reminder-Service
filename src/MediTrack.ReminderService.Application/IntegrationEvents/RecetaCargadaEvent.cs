namespace MediTrack.ReminderService.Application.IntegrationEvents;

/// <summary>
/// Publicado por el Treatment Service al cargar una receta (US13). El Reminder
/// Service lo consume y genera, vía Factory Method, un recordatorio por cada
/// horario de dosis incluido.
/// </summary>
public sealed record RecetaCargadaEvent : IntegrationEvent
{
    public override string EventType => "RecetaCargada";

    public long PatientId { get; init; }

    public long PrescriptionId { get; init; }

    public IReadOnlyList<MedicationScheduleItem> Medications { get; init; } = Array.Empty<MedicationScheduleItem>();
}

/// <summary>Una toma programada de un medicamento dentro de la receta.</summary>
/// <param name="MedicationId">Id del medicamento en el Treatment Service.</param>
/// <param name="Name">Nombre del medicamento.</param>
/// <param name="Dose">Dosis (p. ej. "500 mg").</param>
/// <param name="DoseTimeUtc">Hora exacta de la toma.</param>
public sealed record MedicationScheduleItem(
    long MedicationId,
    string Name,
    string Dose,
    DateTime DoseTimeUtc);
