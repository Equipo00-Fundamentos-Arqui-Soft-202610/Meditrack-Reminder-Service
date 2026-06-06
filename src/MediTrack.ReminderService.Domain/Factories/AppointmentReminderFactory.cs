using MediTrack.ReminderService.Domain.Enums;

namespace MediTrack.ReminderService.Domain.Factories;

/// <summary>
/// Fábrica concreta para recordatorios de citas médicas (US09). Avisa con 24 horas
/// de anticipación e incluye el tipo de cita y, si está disponible, la ubicación.
/// </summary>
public sealed class AppointmentReminderFactory : ReminderFactory
{
    public override ReminderEntityType EntityType => ReminderEntityType.Appointment;

    // La cita se recuerda con un día de anticipación.
    protected override TimeSpan LeadTime => TimeSpan.FromHours(24);

    protected override string BuildTitle(ReminderCreationContext context) =>
        "Recordatorio de cita médica";

    protected override string BuildBody(ReminderCreationContext context) =>
        string.IsNullOrWhiteSpace(context.Detail)
            ? $"Mañana tienes tu cita: {context.Subject}."
            : $"Mañana tienes tu cita: {context.Subject} en {context.Detail}.";
}
