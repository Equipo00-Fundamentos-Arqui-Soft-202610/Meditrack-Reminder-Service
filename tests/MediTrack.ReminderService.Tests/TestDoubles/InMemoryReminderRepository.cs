using MediTrack.ReminderService.Domain.Entities;
using MediTrack.ReminderService.Domain.Enums;
using MediTrack.ReminderService.Domain.Repositories;

namespace MediTrack.ReminderService.Tests.TestDoubles;

/// <summary>Repositorio en memoria para aislar las pruebas de la infraestructura real.</summary>
public sealed class InMemoryReminderRepository : IReminderRepository
{
    private readonly List<Reminder> _store = new();
    private long _sequence;

    public IReadOnlyList<Reminder> Store => _store;

    public Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default)
    {
        AssignId(reminder, ++_sequence);
        _store.Add(reminder);
        return Task.CompletedTask;
    }

    public Task<Reminder?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.FirstOrDefault(r => r.Id == id));

    public Task<IReadOnlyList<Reminder>> GetActiveByPatientAsync(int patientId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Reminder>>(_store
            .Where(r => r.PatientId == patientId && r.Status == ReminderStatus.Scheduled)
            .OrderBy(r => r.ScheduledAt)
            .ToList());

    public Task<IReadOnlyList<Reminder>> GetDueAsync(DateTime asOfUtc, int batchSize, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Reminder>>(_store
            .Where(r => r.Status == ReminderStatus.Scheduled && r.ScheduledAt <= asOfUtc)
            .OrderBy(r => r.ScheduledAt)
            .Take(batchSize)
            .ToList());

    public Task<IReadOnlyList<Reminder>> GetScheduledByEntityAsync(
        int patientId, ReminderEntityType entityType, long entityId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Reminder>>(_store
            .Where(r => r.PatientId == patientId
                        && r.EntityType == entityType
                        && r.EntityId == entityId
                        && r.Status == ReminderStatus.Scheduled)
            .ToList());

    public void Update(Reminder reminder) { /* el agregado ya vive en la lista en memoria */ }

    private static void AssignId(Reminder reminder, long id) =>
        typeof(Reminder).GetProperty(nameof(Reminder.Id))!.SetValue(reminder, id);
}
