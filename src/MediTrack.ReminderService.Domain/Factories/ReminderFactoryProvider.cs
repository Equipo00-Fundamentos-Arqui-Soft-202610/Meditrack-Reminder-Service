using MediTrack.ReminderService.Domain.Enums;

namespace MediTrack.ReminderService.Domain.Factories;

/// <summary>
/// Implementación por defecto del selector de fábricas. Indexa las fábricas
/// disponibles por su <see cref="ReminderFactory.EntityType"/>, de modo que añadir
/// un nuevo tipo solo requiere registrar una nueva fábrica (Open/Closed).
/// </summary>
public sealed class ReminderFactoryProvider : IReminderFactoryProvider
{
    private readonly IReadOnlyDictionary<ReminderEntityType, ReminderFactory> _factories;

    public ReminderFactoryProvider(IEnumerable<ReminderFactory> factories)
    {
        ArgumentNullException.ThrowIfNull(factories);
        _factories = factories.ToDictionary(f => f.EntityType);
    }

    public ReminderFactory For(ReminderEntityType entityType)
    {
        if (_factories.TryGetValue(entityType, out var factory))
            return factory;

        throw new NotSupportedException(
            $"No existe una fábrica de recordatorios para el tipo '{entityType}'.");
    }
}
