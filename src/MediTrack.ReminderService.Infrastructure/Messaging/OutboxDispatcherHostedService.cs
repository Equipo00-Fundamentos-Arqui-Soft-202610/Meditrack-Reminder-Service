using System.Text;
using MediTrack.ReminderService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MediTrack.ReminderService.Infrastructure.Messaging;

/// <summary>
/// Publicador del Outbox: lee periódicamente los mensajes pendientes y los entrega a
/// RabbitMQ usando el <c>EventType</c> como routing key. Marca cada mensaje como
/// procesado solo tras confirmar la publicación, garantizando entrega "al menos una
/// vez" (los consumidores son idempotentes vía Inbox).
/// </summary>
public sealed class OutboxDispatcherHostedService : BackgroundService
{
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnection _connection;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<OutboxDispatcherHostedService> _logger;

    public OutboxDispatcherHostedService(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnection connection,
        IOptions<RabbitMqOptions> options,
        ILogger<OutboxDispatcherHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _connection = connection;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var period = TimeSpan.FromSeconds(Math.Max(1, _options.OutboxPollingSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error en el ciclo del publicador de Outbox.");
            }

            await Task.Delay(period, stoppingToken);
        }
    }

    private async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReminderDbContext>();

        var pending = await context.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null)
            .OrderBy(m => m.OccurredAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return;

        using var channel = _connection.CreateConfiguredChannel();
        channel.ConfirmSelect();

        var unroutableMessageIds = new HashSet<string>();
        channel.BasicReturn += (_, args) =>
        {
            unroutableMessageIds.Add(args.BasicProperties.MessageId);
            _logger.LogError(
                "Mensaje de Outbox {MessageId} no pudo ser ruteado (sin binding activo): {ReplyText}",
                args.BasicProperties.MessageId, args.ReplyText);
        };

        foreach (var message in pending)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(message.Payload);
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = message.Id.ToString();
                properties.Type = message.EventType;
                properties.ContentType = "application/json";
                if (!string.IsNullOrEmpty(message.CorrelationId))
                    properties.CorrelationId = message.CorrelationId;

                channel.BasicPublish(_options.ExchangeName, message.EventType, mandatory: true, properties, body);
                channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

                if (unroutableMessageIds.Contains(properties.MessageId))
                {
                    message.Attempts++;
                    message.LastError = "Sin cola/binding activo en el momento del publish; se reintentará.";
                }
                else
                {
                    message.ProcessedAtUtc = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.LastError = ex.Message;
                _logger.LogError(ex, "No se pudo publicar el mensaje de Outbox {MessageId}.", message.Id);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Publicados {Count} mensajes del Outbox.", pending.Count(m => m.ProcessedAtUtc != null));
    }
}
