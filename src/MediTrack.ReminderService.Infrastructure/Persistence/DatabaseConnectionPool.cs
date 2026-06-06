namespace MediTrack.ReminderService.Infrastructure.Persistence;

/// <summary>
/// Punto único de acceso a la cadena de conexión del pool de MySQL (patrón
/// Singleton, sección 4.1.6). Se registra como Singleton en el contenedor y, junto
/// con <c>AddDbContextPool</c>, asegura que exista un único pool de conexiones
/// durante todo el ciclo de vida del servicio, evitando el agotamiento de recursos.
/// </summary>
public sealed class DatabaseConnectionPool
{
    public DatabaseConnectionPool(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("La cadena de conexión de MySQL es obligatoria.", nameof(connectionString));

        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }
}
