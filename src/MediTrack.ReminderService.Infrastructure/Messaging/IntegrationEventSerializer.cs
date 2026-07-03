using System.Text.Json;
using System.Text.Json.Serialization;
using MediTrack.ReminderService.Application.IntegrationEvents;

namespace MediTrack.ReminderService.Infrastructure.Messaging;

/// <summary>
/// Serializa y deserializa eventos de integración entre objeto y JSON. Mantiene un
/// registro <c>EventType → Type</c> para reconstruir el tipo concreto a partir del
/// routing key recibido desde RabbitMQ.
/// </summary>
public sealed class IntegrationEventSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
    };

    private static readonly IReadOnlyDictionary<string, Type> InboundTypes = new Dictionary<string, Type>
    {
        ["RecetaCargada"] = typeof(RecetaCargadaEvent),
        ["CitaAgendada"] = typeof(CitaAgendadaEvent),
        ["CumplimientoRegistrado"] = typeof(CumplimientoRegistradoEvent),
        ["StockBajo"] = typeof(StockBajoEvent),
        ["ExamenCreado"] = typeof(ExamenCreadoEvent)
    };

    public string Serialize(IntegrationEvent integrationEvent) =>
        JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonOptions);

    public bool TryResolveType(string eventType, out Type type) =>
        InboundTypes.TryGetValue(eventType, out type!);

    public IntegrationEvent? Deserialize(string eventType, string payload)
    {
        if (!TryResolveType(eventType, out var type))
            return null;

        return (IntegrationEvent?)JsonSerializer.Deserialize(payload, type, JsonOptions);
    }
}
