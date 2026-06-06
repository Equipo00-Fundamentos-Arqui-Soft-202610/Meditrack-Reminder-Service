using MediTrack.ReminderService.Domain.Entities;
using MediTrack.ReminderService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.ReminderService.Infrastructure.Persistence;

/// <summary>
/// Contexto EF Core del Reminder Service. Cada microservicio posee su propio
/// esquema MySQL aislado (patrón Database per Service, CON-01). Implementa
/// <see cref="IUnitOfWork"/> para confirmar de forma atómica el agregado y el Outbox.
/// </summary>
public class ReminderDbContext : DbContext, IUnitOfWork
{
    public ReminderDbContext(DbContextOptions<ReminderDbContext> options) : base(options) { }

    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReminderDbContext).Assembly);
    }
}
