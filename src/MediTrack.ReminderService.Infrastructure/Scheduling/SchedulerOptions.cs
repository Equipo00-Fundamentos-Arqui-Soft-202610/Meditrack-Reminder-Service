namespace MediTrack.ReminderService.Infrastructure.Scheduling;

/// <summary>Configuración del Scheduler de recordatorios.</summary>
public sealed class SchedulerOptions
{
    public const string SectionName = "Scheduler";

    /// <summary>Segundos entre cada barrido de recordatorios vencidos.</summary>
    public int PollingSeconds { get; set; } = 15;

    /// <summary>Máximo de recordatorios procesados por ciclo.</summary>
    public int BatchSize { get; set; } = 100;
}
