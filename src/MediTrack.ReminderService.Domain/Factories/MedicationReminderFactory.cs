using MediTrack.ReminderService.Domain.Enums;

namespace MediTrack.ReminderService.Domain.Factories;

/// <summary>
/// Fábrica concreta para recordatorios de medicación (US05). El aviso se dispara
/// justo en la hora de la dosis (sin anticipación) e indica el medicamento y la dosis.
/// </summary>
public sealed class MedicationReminderFactory : ReminderFactory
{
    public override ReminderEntityType EntityType => ReminderEntityType.Medication;

    // La dosis se recuerda en el momento exacto de la toma.
    protected override TimeSpan LeadTime => TimeSpan.Zero;

    protected override string BuildTitle(ReminderCreationContext context) =>
        "Hora de tu medicamento";

    protected override string BuildBody(ReminderCreationContext context) =>
        string.IsNullOrWhiteSpace(context.Detail)
            ? $"Es momento de tomar {context.Subject}."
            : $"Es momento de tomar {context.Subject} ({context.Detail}).";
}
