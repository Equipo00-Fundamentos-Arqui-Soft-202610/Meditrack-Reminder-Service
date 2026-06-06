using MediTrack.ReminderService.Application.IntegrationEvents;

namespace MediTrack.ReminderService.Application.Abstractions;

/// <summary>
/// Puerto para publicar eventos de integración hacia el Message Bus (RabbitMQ).
/// La implementación encola el evento en el Outbox dentro de la misma transacción
/// local (patrón Outbox, AC-08); un publicador en background lo entrega luego.
/// </summary>
public interface IIntegrationEventPublisher
{
    Task EnqueueAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
