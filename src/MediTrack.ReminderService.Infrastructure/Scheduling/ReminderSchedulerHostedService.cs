using MediTrack.ReminderService.Application.Abstractions;
using MediTrack.ReminderService.Application.Services;
using MediTrack.ReminderService.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediTrack.ReminderService.Infrastructure.Scheduling;

/// <summary>
/// "Scheduler" (Fig. 17): gestiona la cola de recordatorios pendientes. En cada
/// ciclo obtiene los recordatorios vencidos y delega su envío en el
/// <see cref="NotificationApplicationService"/>, que aplica preferencias, reintentos
/// y fallback. Es el motor del envío programado de notificaciones.
/// </summary>
public sealed class ReminderSchedulerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SchedulerOptions _options;
    private readonly ILogger<ReminderSchedulerHostedService> _logger;

    public ReminderSchedulerHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<SchedulerOptions> options,
        ILogger<ReminderSchedulerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var period = TimeSpan.FromSeconds(Math.Max(1, _options.PollingSeconds));
        _logger.LogInformation("Scheduler de recordatorios iniciado (cada {Seconds}s).", _options.PollingSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueRemindersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error en el ciclo del Scheduler.");
            }

            await Task.Delay(period, stoppingToken);
        }
    }

    private async Task ProcessDueRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var reminders = scope.ServiceProvider.GetRequiredService<IReminderRepository>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var notifier = scope.ServiceProvider.GetRequiredService<NotificationApplicationService>();

        var due = await reminders.GetDueAsync(clock.UtcNow, _options.BatchSize, cancellationToken);
        if (due.Count == 0)
            return;

        _logger.LogInformation("Procesando {Count} recordatorios vencidos.", due.Count);

        foreach (var reminder in due)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await notifier.DispatchAsync(reminder, cancellationToken);
        }
    }
}
