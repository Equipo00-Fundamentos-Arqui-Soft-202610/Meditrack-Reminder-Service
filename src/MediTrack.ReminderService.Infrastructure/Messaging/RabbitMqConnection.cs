using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediTrack.ReminderService.Infrastructure.Messaging;

/// <summary>
/// Gestiona una conexión perezosa y reutilizable a RabbitMQ (Singleton). Declara la
/// topología (exchange topic + cola del servicio con sus bindings) una sola vez.
/// </summary>
public sealed class RabbitMqConnection : IDisposable
{
    private static readonly string[] ConsumedRoutingKeys =
    {
        "RecetaCargada", "CitaAgendada", "CumplimientoRegistrado", "StockBajo"
    };

    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly object _gate = new();
    private IConnection? _connection;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnection> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string ExchangeName => _options.ExchangeName;
    public string QueueName => _options.QueueName;

    public IConnection GetConnection()
    {
        if (_connection is { IsOpen: true })
            return _connection;

        lock (_gate)
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true
            };

            _connection = factory.CreateConnection("meditrack-reminder-service");
            _logger.LogInformation("Conexión a RabbitMQ establecida en {Host}:{Port}.", _options.Host, _options.Port);
            return _connection;
        }
    }

    /// <summary>Crea un canal con la topología declarada (idempotente).</summary>
    public IModel CreateConfiguredChannel()
    {
        var channel = GetConnection().CreateModel();
        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.QueueDeclare(_options.QueueName, durable: true, exclusive: false, autoDelete: false);

        foreach (var routingKey in ConsumedRoutingKeys)
            channel.QueueBind(_options.QueueName, _options.ExchangeName, routingKey);

        return channel;
    }

    public void Dispose()
    {
        if (_connection is not null)
        {
            _connection.Dispose();
            _connection = null;
        }
    }
}
