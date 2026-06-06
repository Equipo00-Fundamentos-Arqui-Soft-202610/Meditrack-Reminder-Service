namespace MediTrack.ReminderService.Application.Abstractions;

/// <summary>
/// Abstracción de la espera entre reintentos (backoff). En producción usa
/// Task.Delay; en pruebas se sustituye por una implementación instantánea.
/// </summary>
public interface IDelayProvider
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
}
