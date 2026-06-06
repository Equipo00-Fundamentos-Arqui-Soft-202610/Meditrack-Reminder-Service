using MediTrack.ReminderService.Application.Dtos;
using MediTrack.ReminderService.Domain.Entities;
using MediTrack.ReminderService.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MediTrack.ReminderService.Application.Services;

/// <summary>
/// "Preference Application Service" (Fig. 17): gestiona las preferencias de
/// notificación por paciente (US22). Si un paciente no tiene configuración, expone
/// los valores por defecto sin persistirlos hasta que decida cambiarlos.
/// </summary>
public sealed class PreferenceApplicationService
{
    private readonly INotificationPreferenceRepository _preferences;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PreferenceApplicationService> _logger;

    public PreferenceApplicationService(
        INotificationPreferenceRepository preferences,
        IUnitOfWork unitOfWork,
        ILogger<PreferenceApplicationService> logger)
    {
        _preferences = preferences;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<NotificationPreferenceDto> GetAsync(long patientId, CancellationToken cancellationToken = default)
    {
        var preference = await _preferences.GetByPatientAsync(patientId, cancellationToken)
                         ?? NotificationPreference.Default(patientId);
        return NotificationPreferenceDto.FromEntity(preference);
    }

    public async Task<NotificationPreferenceDto> UpsertAsync(
        long patientId, UpdateNotificationPreferenceRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _preferences.GetByPatientAsync(patientId, cancellationToken);

        if (existing is null)
        {
            var created = NotificationPreference.Default(patientId);
            created.Update(request.SoundEnabled, request.VibrationEnabled, request.RepeatCount, request.GlobalEnabled);
            await _preferences.AddAsync(created, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Preferencias creadas para el paciente {PatientId}.", patientId);
            return NotificationPreferenceDto.FromEntity(created);
        }

        existing.Update(request.SoundEnabled, request.VibrationEnabled, request.RepeatCount, request.GlobalEnabled);
        _preferences.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Preferencias actualizadas para el paciente {PatientId}.", patientId);
        return NotificationPreferenceDto.FromEntity(existing);
    }
}
