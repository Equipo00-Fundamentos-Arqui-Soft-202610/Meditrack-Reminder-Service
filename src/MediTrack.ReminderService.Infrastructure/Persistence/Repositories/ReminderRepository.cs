using MediTrack.ReminderService.Domain.Entities;
using MediTrack.ReminderService.Domain.Enums;
using MediTrack.ReminderService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.ReminderService.Infrastructure.Persistence.Repositories;

/// <summary>Implementación EF Core del repositorio de recordatorios (patrón Repository).</summary>
public sealed class ReminderRepository : IReminderRepository
{
    private readonly ReminderDbContext _context;

    public ReminderRepository(ReminderDbContext context) => _context = context;

    public async Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default) =>
        await _context.Reminders.AddAsync(reminder, cancellationToken);

    public Task<Reminder?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _context.Reminders.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Reminder>> GetActiveByPatientAsync(int patientId, CancellationToken cancellationToken = default) =>
        await _context.Reminders
            .Where(r => r.PatientId == patientId && r.Status == ReminderStatus.Scheduled)
            .OrderBy(r => r.ScheduledAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Reminder>> GetDueAsync(DateTime asOfUtc, int batchSize, CancellationToken cancellationToken = default) =>
        await _context.Reminders
            .Where(r => r.Status == ReminderStatus.Scheduled && r.ScheduledAt <= asOfUtc)
            .OrderBy(r => r.ScheduledAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Reminder>> GetScheduledByEntityAsync(
        int patientId, ReminderEntityType entityType, long entityId, CancellationToken cancellationToken = default) =>
        await _context.Reminders
            .Where(r => r.PatientId == patientId
                        && r.EntityType == entityType
                        && r.EntityId == entityId
                        && r.Status == ReminderStatus.Scheduled)
            .ToListAsync(cancellationToken);

    public void Update(Reminder reminder) => _context.Reminders.Update(reminder);
}
