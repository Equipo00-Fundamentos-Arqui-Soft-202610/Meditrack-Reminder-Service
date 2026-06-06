using MediTrack.ReminderService.Application.Abstractions;

namespace MediTrack.ReminderService.Infrastructure.Time;

/// <summary>Reloj real del sistema, en UTC.</summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
