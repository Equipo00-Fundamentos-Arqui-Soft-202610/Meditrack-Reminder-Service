using System.Text;
using System.Text.Json;
using MediTrack.ReminderService.Application.IntegrationEvents;
using MediTrack.ReminderService.Application.Services;
using MediTrack.ReminderService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MediTrack.ReminderService.Infrastructure.Messaging;

/// <summary>
/// "Event Consumer" (Fig. 17): consume los eventos del Message Bus y delega en el
/// <see cref="ScheduleApplicationService"/>. Garantiza idempotencia con el Inbox
/// (<see cref="ProcessedEvent"/>): un mensaje reentregado no se procesa dos veces.
/// </summary>
public sealed class RabbitMqEventConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnection _connection;
    private readonly IntegrationEventSerializer _serializer;
    private readonly ILogger<RabbitMqEventConsumerHostedService> _logger;
    private IModel? _channel;

    public RabbitMqEventConsumerHostedService(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnection connection,
        IntegrationEventSerializer serializer,
        ILogger<RabbitMqEventConsumerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _connection = connection;
        _serializer = serializer;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.CreateConfiguredChannel();
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnReceivedAsync;
        _channel.BasicConsume(_connection.QueueName, autoAck: false, consumer);

        _logger.LogInformation("Event Consumer escuchando la cola {Queue}.", _connection.QueueName);
        return Task.CompletedTask;
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        var eventType = string.IsNullOrEmpty(args.BasicProperties.Type)
            ? args.RoutingKey
            : args.BasicProperties.Type;
        var payload = Encoding.UTF8.GetString(args.Body.Span);

        try
        {
            var integrationEvent = _serializer.Deserialize(eventType, payload);
            if (integrationEvent is null)
            {
                _logger.LogWarning("Evento desconocido '{EventType}' descartado.", eventType);
                _channel!.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            var validationError = ValidateRequiredFields(integrationEvent);
            if (validationError is not null)
            {
                // Datos incompletos (p. ej. un campo requerido en null) no son un
                // fallo transitorio: reencolar solo repetiría el mismo error para
                // siempre. Se descarta y se deja registrado para investigar al productor.
                _logger.LogError(
                    "Evento '{EventType}' con datos inválidos: {Reason}. Se descarta sin reintentar.",
                    eventType, validationError);
                _channel!.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            await ProcessAsync(integrationEvent);
            _channel!.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "Evento '{EventType}' con payload inválido, no se puede deserializar. Se descarta (no es recuperable con reintentos).",
                eventType);
            _channel!.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando el evento '{EventType}'. Se reencolará.", eventType);
            _channel!.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
        }
    }

    /// <summary>
    /// Chequeo defensivo de campos requeridos antes de invocar al handler. Un JSON
    /// válido puede igual traer un campo requerido en null (p. ej. "medications":
    /// null en RecetaCargadaEvent) — sin esto, el handler revienta con
    /// NullReferenceException y el mensaje reencola para siempre.
    /// </summary>
    private static string? ValidateRequiredFields(IntegrationEvent integrationEvent) => integrationEvent switch
    {
        RecetaCargadaEvent e when e.Medications is null => "Medications es null",
        CitaAgendadaEvent e when string.IsNullOrWhiteSpace(e.AppointmentType) => "AppointmentType es nulo o vacío",
        CumplimientoRegistradoEvent e when e.EntityId <= 0 => "EntityId inválido",
        StockBajoEvent e when string.IsNullOrWhiteSpace(e.MedicationName) => "MedicationName es nulo o vacío",
        ExamenCreadoEvent e when string.IsNullOrWhiteSpace(e.ExamType) => "ExamType es nulo o vacío",
        _ => null
    };

    private async Task ProcessAsync(IntegrationEvent integrationEvent)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReminderDbContext>();

        // Inbox: si ya se procesó este EventId, no se vuelve a aplicar.
        var alreadyProcessed = await context.ProcessedEvents
            .AnyAsync(e => e.EventId == integrationEvent.EventId);
        if (alreadyProcessed)
        {
            _logger.LogDebug("Evento {EventId} ya procesado; se omite.", integrationEvent.EventId);
            return;
        }

        var schedule = scope.ServiceProvider.GetRequiredService<ScheduleApplicationService>();

        switch (integrationEvent)
        {
            case RecetaCargadaEvent e:
                await schedule.HandleRecetaCargadaAsync(e);
                break;
            case CitaAgendadaEvent e:
                await schedule.HandleCitaAgendadaAsync(e);
                break;
            case CumplimientoRegistradoEvent e:
                await schedule.HandleCumplimientoRegistradoAsync(e);
                break;
            case StockBajoEvent e:
                await schedule.HandleStockBajoAsync(e);
                break;
            case ExamenCreadoEvent e:
                await schedule.HandleExamenCreadoAsync(e);
                break;
            default:
                _logger.LogWarning("Sin manejador para el evento {EventType}.", integrationEvent.EventType);
                return;
        }

        context.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = integrationEvent.EventId,
            EventType = integrationEvent.EventType,
            ProcessedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
