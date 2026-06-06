using MediTrack.ReminderService.Domain.Enums;

namespace MediTrack.ReminderService.Domain.Factories;

/// <summary>
/// Fábrica concreta para recordatorios de exámenes clínicos (US12). Avisa con 2 horas
/// de anticipación a la fecha de recojo de resultados, indicando el tipo de examen.
/// </summary>
public sealed class ExamReminderFactory : ReminderFactory
{
    public override ReminderEntityType EntityType => ReminderEntityType.Exam;

    // El recojo del examen se recuerda con un par de horas de anticipación.
    protected override TimeSpan LeadTime => TimeSpan.FromHours(2);

    protected override string BuildTitle(ReminderCreationContext context) =>
        "Recordatorio de examen clínico";

    protected override string BuildBody(ReminderCreationContext context) =>
        $"Tienes pendiente tu examen: {context.Subject}. No olvides recoger tus resultados.";
}
