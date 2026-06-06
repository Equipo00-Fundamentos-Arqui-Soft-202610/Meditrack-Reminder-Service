namespace MediTrack.ReminderService.Domain.Enums;

/// <summary>
/// Tipo de entidad clínica que origina un recordatorio. Determina qué fábrica
/// (Factory Method) construye el recordatorio. Ver sección 4.1.6 del informe.
/// </summary>
public enum ReminderEntityType
{
    Medication = 0,
    Appointment = 1,
    Exam = 2
}
