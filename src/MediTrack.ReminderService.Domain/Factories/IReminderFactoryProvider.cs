using MediTrack.ReminderService.Domain.Enums;

namespace MediTrack.ReminderService.Domain.Factories;

/// <summary>
/// Selector de fábricas: dado un <see cref="ReminderEntityType"/> devuelve la
/// fábrica especializada correspondiente. Implementa la frase del informe
/// "Dependiendo del tipo de entidad se instancia una fábrica especializada".
/// </summary>
public interface IReminderFactoryProvider
{
    ReminderFactory For(ReminderEntityType entityType);
}
