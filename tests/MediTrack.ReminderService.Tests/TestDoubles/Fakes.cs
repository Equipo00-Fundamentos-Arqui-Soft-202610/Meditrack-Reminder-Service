using MediTrack.ReminderService.Application.Abstractions;
using MediTrack.ReminderService.Application.IntegrationEvents;
using MediTrack.ReminderService.Domain.Repositories;

namespace MediTrack.ReminderService.Tests.TestDoubles;

/// <summary>Reloj fijo controlable en pruebas.</summary>
public sealed class FakeClock : IClock
{
    public FakeClock(DateTime utcNow) => UtcNow = utcNow;
    public DateTime UtcNow { get; set; }
}

/// <summary>Backoff instantáneo: no espera realmente entre reintentos.</summary>
public sealed class ImmediateDelayProvider : IDelayProvider
{
    public int DelayCount { get; private set; }

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        DelayCount++;
        return Task.CompletedTask;
    }
}

/// <summary>Sender configurable que registra cuántas veces se le llamó.</summary>
public sealed class FakeNotificationSender : INotificationSender
{
    private readonly Func<int, bool> _successByAttempt;

    public FakeNotificationSender(Func<int, bool> successByAttempt) => _successByAttempt = successByAttempt;

    public static FakeNotificationSender AlwaysSucceeds() => new(_ => true);
    public static FakeNotificationSender AlwaysFails() => new(_ => false);

    public int Attempts { get; private set; }
    public List<NotificationMessage> SentMessages { get; } = new();

    public Task<NotificationDeliveryResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var current = Attempts++;
        SentMessages.Add(message);
        return Task.FromResult(_successByAttempt(current)
            ? NotificationDeliveryResult.Ok("fake")
            : NotificationDeliveryResult.Fail("fake failure"));
    }
}

/// <summary>Publicador que acumula los eventos encolados al Outbox.</summary>
public sealed class RecordingEventPublisher : IIntegrationEventPublisher
{
    public List<IntegrationEvent> Published { get; } = new();

    public Task EnqueueAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        Published.Add(integrationEvent);
        return Task.CompletedTask;
    }
}

/// <summary>UnitOfWork que cuenta confirmaciones sin persistir.</summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(0);
    }
}
