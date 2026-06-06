namespace MediTrack.ReminderService.Application.Abstractions;

/// <summary>
/// Abstracción del reloj del sistema. Permite controlar el tiempo en pruebas
/// (evita dependencias ocultas de DateTime.UtcNow).
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
