using MediTrack.ReminderService.Application.Abstractions;
using MediTrack.ReminderService.Application.IntegrationEvents;
using MediTrack.ReminderService.Infrastructure.Persistence;

namespace MediTrack.ReminderService.Infrastructure.Messaging;

/// <summary>
/// Implementación del puerto de publicación basada en el patrón Outbox (AC-08). En
/// lugar de publicar directamente a RabbitMQ, persiste el evento como
/// <see cref="OutboxMessage"/> en el MISMO DbContext que el cambio de dominio, de
/// modo que ambos se confirman en una única transacción local. El
/// <see cref="OutboxDispatcherHostedService"/> lo entrega luego al broker.
/// </summary>
public sealed class OutboxIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly ReminderDbContext _context;
    private readonly IntegrationEventSerializer _serializer;

    public OutboxIntegrationEventPublisher(ReminderDbContext context, IntegrationEventSerializer serializer)
    {
        _context = context;
        _serializer = serializer;
    }

    public async Task EnqueueAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var message = new OutboxMessage
        {
            Id = integrationEvent.EventId,
            EventType = integrationEvent.EventType,
            Payload = _serializer.Serialize(integrationEvent),
            CorrelationId = integrationEvent.CorrelationId,
            OccurredAtUtc = integrationEvent.OccurredAtUtc,
            Attempts = 0
        };

        await _context.OutboxMessages.AddAsync(message, cancellationToken);
        // No se llama SaveChanges aquí: lo confirma la UnitOfWork del caso de uso.
    }
}
