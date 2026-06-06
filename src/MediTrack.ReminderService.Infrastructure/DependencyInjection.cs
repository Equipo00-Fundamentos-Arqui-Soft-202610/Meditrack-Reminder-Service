using MediTrack.ReminderService.Application.Abstractions;
using MediTrack.ReminderService.Domain.Repositories;
using MediTrack.ReminderService.Infrastructure.Messaging;
using MediTrack.ReminderService.Infrastructure.Notifications;
using MediTrack.ReminderService.Infrastructure.Persistence;
using MediTrack.ReminderService.Infrastructure.Persistence.Repositories;
using MediTrack.ReminderService.Infrastructure.Scheduling;
using MediTrack.ReminderService.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MediTrack.ReminderService.Infrastructure;

/// <summary>
/// Registro de la capa de infraestructura: persistencia (EF Core + MySQL bajo
/// Database per Service), repositorios, mensajería RabbitMQ con Outbox/Inbox, el
/// adaptador de FCM y los servicios en segundo plano (Scheduler, Consumer, Outbox).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddPersistence(services, configuration);
        AddMessaging(services, configuration);
        AddNotifications(services, configuration);
        AddBackgroundServices(services, configuration);

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IDelayProvider, TaskDelayProvider>();

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ReminderDb")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'ReminderDb'.");

        // Patrón Singleton (4.1.6): un único punto de acceso a la cadena del pool.
        services.AddSingleton(new DatabaseConnectionPool(connectionString));

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
        services.AddDbContextPool<ReminderDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, mySql =>
            {
                mySql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
                mySql.MigrationsAssembly(typeof(ReminderDbContext).Assembly.FullName);
            }));

        services.AddScoped<IReminderRepository, ReminderRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ReminderDbContext>());
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IntegrationEventSerializer>();
        services.AddSingleton<RabbitMqConnection>();
        services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();
    }

    private static void AddNotifications(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FcmOptions>(configuration.GetSection(FcmOptions.SectionName));
        services.AddSingleton<INotificationSender, FcmNotificationSender>();
    }

    private static void AddBackgroundServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SchedulerOptions>(configuration.GetSection(SchedulerOptions.SectionName));
        services.AddHostedService<RabbitMqEventConsumerHostedService>();
        services.AddHostedService<OutboxDispatcherHostedService>();
        services.AddHostedService<ReminderSchedulerHostedService>();
    }
}
