using MediTrack.ReminderService.Domain.Entities;
using MediTrack.ReminderService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.ReminderService.Infrastructure.Persistence.Repositories;

/// <summary>Implementación EF Core del repositorio de preferencias de notificación.</summary>
public sealed class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly ReminderDbContext _context;

    public NotificationPreferenceRepository(ReminderDbContext context) => _context = context;

    public Task<NotificationPreference?> GetByPatientAsync(int patientId, CancellationToken cancellationToken = default) =>
        _context.NotificationPreferences.FirstOrDefaultAsync(p => p.PatientId == patientId, cancellationToken);

    public async Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default) =>
        await _context.NotificationPreferences.AddAsync(preference, cancellationToken);

    public void Update(NotificationPreference preference) => _context.NotificationPreferences.Update(preference);
}
