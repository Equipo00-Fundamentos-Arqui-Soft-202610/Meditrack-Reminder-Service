namespace MediTrack.ReminderService.Domain.Factories;

/// <summary>
/// Datos de entrada para construir un recordatorio, extraídos del evento de
/// integración que lo origina (RecetaCargada, CitaAgendada, etc.). Es un objeto
/// inmutable que desacopla las fábricas del transporte de mensajería.
/// </summary>
/// <param name="PatientId">Paciente destinatario.</param>
/// <param name="EntityId">Id de la entidad origen en su microservicio.</param>
/// <param name="EventTimeUtc">Instante clínico de referencia (hora de dosis, de la cita o de recojo del examen).</param>
/// <param name="Subject">Texto descriptivo principal (nombre del medicamento, tipo de cita o examen).</param>
/// <param name="Detail">Detalle opcional (dosis, ubicación, indicaciones).</param>
public sealed record ReminderCreationContext(
    int PatientId,
    long EntityId,
    DateTime EventTimeUtc,
    string Subject,
    string? Detail = null);
