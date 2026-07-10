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
        "RecetaCargada", "CitaAgendada", "CumplimientoRegistrado", "StockBajo",
        "ExamenCreado"
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
                AutomaticRecoveryEnabled = true,
                RequestedHeartbeat = TimeSpan.FromSeconds(30),
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
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

        var dlxName = $"{_options.ExchangeName}.dlx";
        var dlqName = $"{_options.QueueName}.dlq";
        channel.ExchangeDeclare(dlxName, ExchangeType.Fanout, durable: true);
        channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(dlqName, dlxName, routingKey: "");

        var queueArgs = new Dictionary<string, object> { { "x-dead-letter-exchange", dlxName } };
        try
        {
            channel.QueueDeclare(_options.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        }
        catch (RabbitMQ.Client.Exceptions.OperationInterruptedException)
        {
            _logger.LogWarning(
                "La cola {QueueName} existía con argumentos distintos; se recrea con soporte de DLQ.",
                _options.QueueName);
            channel = GetConnection().CreateModel();
            channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
            channel.ExchangeDeclare(dlxName, ExchangeType.Fanout, durable: true);
            channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false);
            channel.QueueBind(dlqName, dlxName, routingKey: "");
            channel.QueueDelete(_options.QueueName);
            channel.QueueDeclare(_options.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        }

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
