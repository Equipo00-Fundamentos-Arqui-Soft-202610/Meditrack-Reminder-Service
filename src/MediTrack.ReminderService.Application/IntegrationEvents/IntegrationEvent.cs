namespace MediTrack.ReminderService.Application.IntegrationEvents;

/// <summary>
/// Base de los eventos de integración intercambiados por RabbitMQ. Incluye un
/// identificador único para idempotencia (Inbox) y un correlation id para tracing
/// distribuido (AC-11).
/// </summary>
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;

    public string? CorrelationId { get; init; }

    /// <summary>Nombre estable usado como routing key en RabbitMQ.</summary>
    public abstract string EventType { get; }
}
