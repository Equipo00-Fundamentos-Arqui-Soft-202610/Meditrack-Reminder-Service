using MediTrack.ReminderService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MediTrack.ReminderService.Api.Persistence;

/// <summary>
/// Fábrica en tiempo de diseño para que la herramienta <c>dotnet ef</c> pueda crear
/// y aplicar migraciones sin arrancar todo el host (RabbitMQ, hosted services, etc.).
/// Toma la cadena de conexión de la variable de entorno
/// <c>ConnectionStrings__ReminderDb</c> o usa un valor por defecto local.
/// </summary>
public sealed class ReminderDbContextFactory : IDesignTimeDbContextFactory<ReminderDbContext>
{
    public ReminderDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ReminderDb")
            ?? "Server=localhost;Port=3306;Database=meditrack_reminder;User=root;Password=root;";

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
        var options = new DbContextOptionsBuilder<ReminderDbContext>()
            .UseMySql(connectionString, serverVersion, mySql =>
                mySql.MigrationsAssembly(typeof(ReminderDbContext).Assembly.FullName))
            .Options;

        return new ReminderDbContext(options);
    }
}
