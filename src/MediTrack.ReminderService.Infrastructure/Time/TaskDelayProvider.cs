using MediTrack.ReminderService.Application.Abstractions;

namespace MediTrack.ReminderService.Infrastructure.Time;

/// <summary>Espera real basada en Task.Delay para el backoff entre reintentos.</summary>
public sealed class TaskDelayProvider : IDelayProvider
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default) =>
        Task.Delay(delay, cancellationToken);
}
