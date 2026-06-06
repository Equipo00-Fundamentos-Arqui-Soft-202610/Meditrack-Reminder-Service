using MediTrack.ReminderService.Application.Abstractions;
using MediTrack.ReminderService.Application.Configuration;
using MediTrack.ReminderService.Application.IntegrationEvents;
using MediTrack.ReminderService.Domain.Entities;
using MediTrack.ReminderService.Domain.Enums;
using MediTrack.ReminderService.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediTrack.ReminderService.Application.Services;

/// <summary>
/// "Notification Application Service" (Fig. 17): orquesta el disparo de la
/// notificación push de un recordatorio vencido. Aplica las preferencias del
/// paciente, ejecuta reintentos con backoff exponencial ante fallos de FCM (AC-02),
/// activa el fallback local (AC-09), registra la bitácora y publica
/// <see cref="RecordatorioEnviadoEvent"/> vía Outbox.
/// </summary>
public sealed class NotificationApplicationService
{
    private readonly IReminderRepository _reminders;
    private readonly INotificationPreferenceRepository _preferences;
    private readonly INotificationSender _sender;
    private readonly IIntegrationEventPublisher _publisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IDelayProvider _delay;
    private readonly ReminderNotificationOptions _options;
    private readonly ILogger<NotificationApplicationService> _logger;

    public NotificationApplicationService(
        IReminderRepository reminders,
        INotificationPreferenceRepository preferences,
        INotificationSender sender,
        IIntegrationEventPublisher publisher,
        IUnitOfWork unitOfWork,
        IClock clock,
        IDelayProvider delay,
        IOptions<ReminderNotificationOptions> options,
        ILogger<NotificationApplicationService> logger)
    {
        _reminders = reminders;
        _preferences = preferences;
        _sender = sender;
        _publisher = publisher;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _delay = delay;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Despacha un recordatorio vencido. Devuelve true si quedó entregado (push o
    /// fallback), false si el paciente tiene las notificaciones desactivadas o si
    /// se agotaron los intentos sin entrega.
    /// </summary>
    public async Task<bool> DispatchAsync(Reminder reminder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        var preference = await _preferences.GetByPatientAsync(reminder.PatientId, cancellationToken)
                         ?? NotificationPreference.Default(reminder.PatientId);

        // US22: interruptor maestro. Si está desactivado, no se envía nada.
        if (!preference.GlobalEnabled)
        {
            _logger.LogInformation(
                "Notificaciones desactivadas para el paciente {PatientId}; se omite el recordatorio {ReminderId}.",
                reminder.PatientId, reminder.Id);
            return false;
        }

        var message = BuildMessage(reminder, preference, NotificationChannel.Push);
        var delivered = await TryDeliverWithRetriesAsync(reminder, message, cancellationToken);

        if (!delivered && _options.EnableLocalFallback)
            delivered = ActivateLocalFallback(reminder);

        if (!delivered)
            reminder.MarkAsFailed();

        _reminders.Update(reminder);
        await PublishOutcomeAsync(reminder, delivered, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return delivered;
    }

    private async Task<bool> TryDeliverWithRetriesAsync(
        Reminder reminder, NotificationMessage message, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < _options.MaxAttempts; attempt++)
        {
            var result = await _sender.SendAsync(message, cancellationToken);

            if (result.Success)
            {
                reminder.RegisterDeliveryAttempt(NotificationChannel.Push, DeliveryStatus.Delivered, _clock.UtcNow);
                _logger.LogInformation(
                    "Recordatorio {ReminderId} entregado vía push en el intento {Attempt}.", reminder.Id, attempt + 1);
                return true;
            }

            reminder.RegisterDeliveryAttempt(NotificationChannel.Push, DeliveryStatus.Failed, _clock.UtcNow);
            _logger.LogWarning(
                "Fallo de entrega push del recordatorio {ReminderId} (intento {Attempt}): {Error}.",
                reminder.Id, attempt + 1, result.Error);

            var isLastAttempt = attempt == _options.MaxAttempts - 1;
            if (!isLastAttempt)
                await _delay.DelayAsync(_options.BackoffFor(attempt), cancellationToken);
        }

        return false;
    }

    private bool ActivateLocalFallback(Reminder reminder)
    {
        // AC-09: la app programa una notificación local sobre su almacenamiento
        // offline. Se registra el handoff como canal de respaldo.
        reminder.RegisterDeliveryAttempt(NotificationChannel.Local, DeliveryStatus.Delivered, _clock.UtcNow);
        _logger.LogInformation(
            "Activado fallback local para el recordatorio {ReminderId} tras agotar los reintentos push.", reminder.Id);
        return true;
    }

    private NotificationMessage BuildMessage(Reminder reminder, NotificationPreference preference, NotificationChannel channel) =>
        new(
            reminder.PatientId,
            $"{_options.FcmTopicPrefix}{reminder.PatientId}",
            reminder.Title,
            reminder.Body,
            preference.SoundEnabled,
            preference.VibrationEnabled,
            channel);

    private async Task PublishOutcomeAsync(Reminder reminder, bool delivered, CancellationToken cancellationToken)
    {
        var lastChannel = reminder.NotificationLogs.LastOrDefault()?.Channel ?? NotificationChannel.Push;
        var @event = new RecordatorioEnviadoEvent
        {
            ReminderId = reminder.Id,
            PatientId = reminder.PatientId,
            EntityType = reminder.EntityType,
            EntityId = reminder.EntityId,
            Channel = lastChannel,
            Delivered = delivered,
            SentAtUtc = _clock.UtcNow
        };
        await _publisher.EnqueueAsync(@event, cancellationToken);
    }
}
