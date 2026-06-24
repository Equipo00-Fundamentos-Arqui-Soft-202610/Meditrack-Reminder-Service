using MediTrack.ReminderService.Domain.Entities;
using MediTrack.ReminderService.Domain.Enums;

namespace MediTrack.ReminderService.Domain.Repositories;

/// <summary>
/// Puerto de persistencia para el agregado Reminder (patrón Repository, 4.1.6).
/// Abstrae el acceso a MySQL detrás de una interfaz limpia y testeable.
/// </summary>
public interface IReminderRepository
{
    Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default);

    Task<Reminder?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Recordatorios activos (programados) de un paciente para GET /reminders/patients/{id}.</summary>
    Task<IReadOnlyList<Reminder>> GetActiveByPatientAsync(int patientId, CancellationToken cancellationToken = default);

    /// <summary>Recordatorios vencidos pendientes de envío, usados por el Scheduler.</summary>
    Task<IReadOnlyList<Reminder>> GetDueAsync(DateTime asOfUtc, int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recordatorios programados que apuntan a una entidad concreta. Usado para
    /// cancelar al recibir CumplimientoRegistrado / cita cancelada.
    /// </summary>
    Task<IReadOnlyList<Reminder>> GetScheduledByEntityAsync(
        int patientId, ReminderEntityType entityType, long entityId, CancellationToken cancellationToken = default);

    void Update(Reminder reminder);
}
