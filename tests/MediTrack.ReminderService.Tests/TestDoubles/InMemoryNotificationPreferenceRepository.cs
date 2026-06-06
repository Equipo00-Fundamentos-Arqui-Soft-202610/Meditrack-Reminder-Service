using MediTrack.ReminderService.Domain.Entities;
using MediTrack.ReminderService.Domain.Repositories;

namespace MediTrack.ReminderService.Tests.TestDoubles;

/// <summary>Repositorio de preferencias en memoria para pruebas.</summary>
public sealed class InMemoryNotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly List<NotificationPreference> _store = new();

    public void Seed(NotificationPreference preference) => _store.Add(preference);

    public Task<NotificationPreference?> GetByPatientAsync(long patientId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.FirstOrDefault(p => p.PatientId == patientId));

    public Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        _store.Add(preference);
        return Task.CompletedTask;
    }

    public void Update(NotificationPreference preference) { /* ya está en la lista */ }
}
