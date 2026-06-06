namespace MediTrack.ReminderService.Infrastructure.Persistence;

/// <summary>
/// Mensaje saliente persistido en la misma transacción local que el cambio de
/// dominio (patrón Outbox, AC-08). Un publicador en background lo entrega luego a
/// RabbitMQ, garantizando que ningún evento se pierda si el broker está caído.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string EventType { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public string? CorrelationId { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}
