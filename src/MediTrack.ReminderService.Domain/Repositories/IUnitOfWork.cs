namespace MediTrack.ReminderService.Domain.Repositories;

/// <summary>
/// Unidad de trabajo: confirma de forma atómica los cambios del agregado junto con
/// los mensajes del Outbox, garantizando consistencia local (patrón Outbox, AC-08).
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
