using MediTrack.ReminderService.Application.Configuration;
using MediTrack.ReminderService.Application.Services;
using MediTrack.ReminderService.Domain.Factories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MediTrack.ReminderService.Application;

/// <summary>
/// Registro de la capa de aplicación: fábricas de recordatorios (Factory Method),
/// el selector de fábricas y los Application Services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ReminderNotificationOptions>(
            configuration.GetSection(ReminderNotificationOptions.SectionName));

        // Fábricas concretas del patrón Factory Method (una por tipo de recordatorio).
        // La fábrica de citas se registra para el proveedor con leadTime 24 h.
        services.AddSingleton<ReminderFactory>(sp =>
            new AppointmentReminderFactory(TimeSpan.FromHours(24), AppointmentReminderFactory.TwentyFourHourBody));
        services.AddSingleton<ReminderFactory, MedicationReminderFactory>();
        services.AddSingleton<ReminderFactory, ExamReminderFactory>();
        services.AddSingleton<IReminderFactoryProvider, ReminderFactoryProvider>();

        // Segunda instancia (2 h) inyectada directamente; no pasa por el proveedor
        // porque comparte EntityType.Appointment.
        services.AddSingleton<AppointmentReminderFactory>(sp =>
            new AppointmentReminderFactory(TimeSpan.FromHours(2), AppointmentReminderFactory.TwoHourBody));

        // Application Services (uno por controlador / responsabilidad).
        services.AddScoped<ScheduleApplicationService>();
        services.AddScoped<NotificationApplicationService>();
        services.AddScoped<PreferenceApplicationService>();

        return services;
    }
}
