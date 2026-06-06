using MediTrack.ReminderService.Domain.Entities;
using MediTrack.ReminderService.Domain.Enums;

namespace MediTrack.ReminderService.Domain.Factories;

/// <summary>
/// Creador abstracto del patrón Factory Method (sección 4.1.6 del informe).
///
/// Define el método fábrica <see cref="CreateReminder"/> como plantilla común y
/// delega en las subclases la decisión de QUÉ tiempo de anticipación aplicar y
/// CÓMO redactar el mensaje. Esto elimina condicionales por tipo y permite
/// incorporar nuevos tipos de recordatorio sin modificar el código existente
/// (principio Open/Closed).
/// </summary>
public abstract class ReminderFactory
{
    /// <summary>Tipo de entidad que esta fábrica sabe construir.</summary>
    public abstract ReminderEntityType EntityType { get; }

    /// <summary>
    /// Tiempo de anticipación con el que se programa el recordatorio respecto al
    /// instante clínico. Cada fábrica define el suyo (p. ej. la cita avisa 24 h antes).
    /// </summary>
    protected abstract TimeSpan LeadTime { get; }

    /// <summary>Construye el título de la notificación para este tipo.</summary>
    protected abstract string BuildTitle(ReminderCreationContext context);

    /// <summary>Construye el cuerpo de la notificación para este tipo.</summary>
    protected abstract string BuildBody(ReminderCreationContext context);

    /// <summary>
    /// Método fábrica: produce un <see cref="Reminder"/> consistente aplicando la
    /// anticipación y el mensaje propios del tipo. Plantilla estable, partes variables
    /// resueltas por la subclase.
    /// </summary>
    public Reminder CreateReminder(ReminderCreationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scheduledAt = context.EventTimeUtc - LeadTime;
        return Reminder.Schedule(
            context.PatientId,
            EntityType,
            context.EntityId,
            scheduledAt,
            BuildTitle(context),
            BuildBody(context));
    }
}
