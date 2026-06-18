using MediTrack.ReminderService.Domain.Entities;

namespace MediTrack.ReminderService.Domain.Repositories;

/// <summary>
/// Puerto de persistencia para las preferencias de notificación por paciente (US22).
/// </summary>
public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetByPatientAsync(int patientId, CancellationToken cancellationToken = default);

    Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default);

    void Update(NotificationPreference preference);
}
