using MediTrack.ReminderService.Domain.Enums;

namespace MediTrack.ReminderService.Domain.Factories;

public sealed class AppointmentReminderFactory : ReminderFactory
{
    private readonly TimeSpan _leadTime;
    private readonly Func<ReminderCreationContext, string> _bodyBuilder;

    public AppointmentReminderFactory(TimeSpan leadTime, Func<ReminderCreationContext, string> bodyBuilder)
    {
        _leadTime = leadTime;
        _bodyBuilder = bodyBuilder;
    }

    public override ReminderEntityType EntityType => ReminderEntityType.Appointment;

    protected override TimeSpan LeadTime => _leadTime;

    protected override string BuildTitle(ReminderCreationContext context) =>
        "Recordatorio de cita médica";

    protected override string BuildBody(ReminderCreationContext context) =>
        _bodyBuilder(context);

    public static string TwentyFourHourBody(ReminderCreationContext context) =>
        string.IsNullOrWhiteSpace(context.Detail)
            ? $"Mañana tienes tu cita: {context.Subject}."
            : $"Mañana tienes tu cita: {context.Subject} en {context.Detail}.";

    public static string TwoHourBody(ReminderCreationContext context) =>
        string.IsNullOrWhiteSpace(context.Detail)
            ? $"Tu cita es en 2 horas: {context.Subject}."
            : $"Tu cita es en 2 horas: {context.Subject} en {context.Detail}.";
}
