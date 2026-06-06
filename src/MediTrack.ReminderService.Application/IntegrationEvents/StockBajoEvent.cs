namespace MediTrack.ReminderService.Application.IntegrationEvents;

/// <summary>
/// Publicado por el Treatment Service cuando el stock de un medicamento alcanza el
/// umbral (US07, 3 unidades). El Reminder Service genera una alerta inmediata al
/// paciente para que reabastezca.
/// </summary>
public sealed record StockBajoEvent : IntegrationEvent
{
    public override string EventType => "StockBajo";

    public long PatientId { get; init; }

    public long MedicationId { get; init; }

    public string MedicationName { get; init; } = string.Empty;

    public int RemainingUnits { get; init; }
}
