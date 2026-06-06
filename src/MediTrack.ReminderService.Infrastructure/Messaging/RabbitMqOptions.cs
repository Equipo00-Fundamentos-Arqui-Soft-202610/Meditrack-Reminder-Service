namespace MediTrack.ReminderService.Infrastructure.Messaging;

/// <summary>
/// Configuración de la conexión y la topología de RabbitMQ (Message Bus, 5.4).
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    /// <summary>Exchange de tipo topic compartido por todos los microservicios.</summary>
    public string ExchangeName { get; set; } = "meditrack.events";

    /// <summary>Cola propia del Reminder Service.</summary>
    public string QueueName { get; set; } = "reminder-service.events";

    /// <summary>Segundos entre ciclos del publicador de Outbox.</summary>
    public int OutboxPollingSeconds { get; set; } = 10;
}
