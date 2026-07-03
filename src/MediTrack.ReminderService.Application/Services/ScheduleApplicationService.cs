using MediTrack.ReminderService.Application.Dtos;
using MediTrack.ReminderService.Application.IntegrationEvents;
using MediTrack.ReminderService.Domain.Entities;
using MediTrack.ReminderService.Domain.Enums;
using MediTrack.ReminderService.Domain.Factories;
using MediTrack.ReminderService.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MediTrack.ReminderService.Application.Services;

/// <summary>
/// "Schedule Application Service" (Fig. 17): orquesta la PROGRAMACIÓN y la
/// CANCELACIÓN de recordatorios en respuesta a los eventos clínicos y a las
/// peticiones REST. Usa el Factory Method para construir cada recordatorio y el
/// Repository para persistirlo.
/// </summary>
public sealed class ScheduleApplicationService
{
    private readonly IReminderRepository _reminders;
    private readonly IReminderFactoryProvider _factories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Abstractions.IClock _clock;
    private readonly ILogger<ScheduleApplicationService> _logger;
    private readonly AppointmentReminderFactory _twoHourFactory;

    public ScheduleApplicationService(
        IReminderRepository reminders,
        IReminderFactoryProvider factories,
        IUnitOfWork unitOfWork,
        Abstractions.IClock clock,
        ILogger<ScheduleApplicationService> logger,
        AppointmentReminderFactory twoHourFactory)
    {
        _reminders = reminders;
        _factories = factories;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
        _twoHourFactory = twoHourFactory;
    }

    /// <summary>Genera un recordatorio de medicación por cada toma de la receta (US05/US13).</summary>
    public async Task HandleRecetaCargadaAsync(RecetaCargadaEvent @event, CancellationToken cancellationToken = default)
    {
        var factory = _factories.For(ReminderEntityType.Medication);

        foreach (var med in @event.Medications)
        {
            var context = new ReminderCreationContext(
                @event.PatientId, med.MedicationId, med.DoseTimeUtc, med.Name, med.Dose);
            var reminder = factory.CreateReminder(context);
            await _reminders.AddAsync(reminder, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Generados {Count} recordatorios de medicación para el paciente {PatientId} (receta {PrescriptionId}).",
            @event.Medications.Count, @event.PatientId, @event.PrescriptionId);
    }

    /// <summary>
    /// Genera dos recordatorios de cita: uno con 24 h de anticipación (US09) y otro
    /// con 2 h de anticipación.
    /// </summary>
    public async Task HandleCitaAgendadaAsync(CitaAgendadaEvent @event, CancellationToken cancellationToken = default)
    {
        var factory = _factories.For(ReminderEntityType.Appointment);
        var context = new ReminderCreationContext(
            @event.PatientId, @event.AppointmentId, @event.AppointmentDateUtc, @event.AppointmentType, @event.Location);

        var reminder24h = factory.CreateReminder(context);
        await _reminders.AddAsync(reminder24h, cancellationToken);

        var reminder2h = _twoHourFactory.CreateReminder(context);
        await _reminders.AddAsync(reminder2h, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Generados recordatorios de cita {AppointmentId} para el paciente {PatientId} (24 h y 2 h).",
            @event.AppointmentId, @event.PatientId);
    }

    /// <summary>Genera un recordatorio de examen con 2 h de anticipación al recojo (US12).</summary>
    public async Task HandleExamenCreadoAsync(ExamenCreadoEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event.PickupDate is null)
        {
            _logger.LogDebug(
                "Examen {ExamId} sin fecha de recojo; no se genera recordatorio.",
                @event.ExamId);
            return;
        }

        var factory = _factories.For(ReminderEntityType.Exam);
        var context = new ReminderCreationContext(
            @event.PatientId, @event.ExamId, @event.PickupDate.Value, @event.ExamType);

        var reminder = factory.CreateReminder(context);
        await _reminders.AddAsync(reminder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Generado recordatorio de examen {ExamId} para el paciente {PatientId}.",
            @event.ExamId, @event.PatientId);
    }

    /// <summary>Genera una alerta inmediata de stock bajo (US07).</summary>
    public async Task HandleStockBajoAsync(StockBajoEvent @event, CancellationToken cancellationToken = default)
    {
        var alert = Reminder.CreateAlert(
            @event.PatientId,
            ReminderEntityType.Medication,
            @event.MedicationId,
            _clock.UtcNow,
            "Stock bajo de medicamento",
            $"Te quedan {@event.RemainingUnits} unidades de {@event.MedicationName}. Recuerda reabastecerte.");

        await _reminders.AddAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Generada alerta de stock bajo del medicamento {MedicationId} para el paciente {PatientId}.",
            @event.MedicationId, @event.PatientId);
    }

    /// <summary>Cancela los recordatorios pendientes de la entidad cumplida (US06, patrón Observer).</summary>
    public async Task HandleCumplimientoRegistradoAsync(CumplimientoRegistradoEvent @event, CancellationToken cancellationToken = default)
    {
        var pending = await _reminders.GetScheduledByEntityAsync(
            @event.PatientId, @event.EntityType, @event.EntityId, cancellationToken);

        if (pending.Count == 0)
        {
            _logger.LogDebug(
                "Sin recordatorios pendientes que cancelar para {EntityType} {EntityId} (paciente {PatientId}).",
                @event.EntityType, @event.EntityId, @event.PatientId);
            return;
        }

        var limaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        var targetLocalDate = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(@event.OccurrenceDateUtc, DateTimeKind.Utc), limaTimeZone).Date;

        var matching = pending
            .Where(r => TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(r.ScheduledAt, DateTimeKind.Utc), limaTimeZone).Date == targetLocalDate)
            .ToList();

        if (matching.Count == 0)
        {
            _logger.LogDebug(
                "Ningún recordatorio pendiente coincide con la fecha {Date} para {EntityType} {EntityId} (paciente {PatientId}). {Total} pendientes en otras fechas no se tocan.",
                targetLocalDate, @event.EntityType, @event.EntityId, @event.PatientId, pending.Count);
            return;
        }

        foreach (var reminder in matching)
        {
            reminder.Cancel(_clock.UtcNow);
            _reminders.Update(reminder);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Cancelados {Count} de {Total} recordatorios pendientes (fecha {Date}) tras cumplimiento de {EntityType} {EntityId}.",
            matching.Count, pending.Count, targetLocalDate, @event.EntityType, @event.EntityId);
    }

    /// <summary>Lista los recordatorios activos del paciente (GET /reminders/patients/{patientId}).</summary>
    public async Task<IReadOnlyList<ReminderDto>> GetActiveByPatientAsync(int patientId, CancellationToken cancellationToken = default)
    {
        var reminders = await _reminders.GetActiveByPatientAsync(patientId, cancellationToken);
        return reminders.Select(ReminderDto.FromEntity).ToList();
    }

    /// <summary>Cancela un recordatorio por id (PUT /reminders/{id}/cancel). Devuelve false si no existe.</summary>
    public async Task<bool> CancelReminderAsync(long reminderId, CancellationToken cancellationToken = default)
    {
        var reminder = await _reminders.GetByIdAsync(reminderId, cancellationToken);
        if (reminder is null)
            return false;

        reminder.Cancel(_clock.UtcNow);
        _reminders.Update(reminder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Recordatorio {ReminderId} cancelado por solicitud.", reminderId);
        return true;
    }
}
