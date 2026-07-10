using FluentAssertions;
using MediTrack.ReminderService.Application.Configuration;
using MediTrack.ReminderService.Application.Services;
using MediTrack.ReminderService.Domain.Entities;
using MediTrack.ReminderService.Domain.Enums;
using MediTrack.ReminderService.Domain.Factories;
using MediTrack.ReminderService.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace MediTrack.ReminderService.Tests.Application;

public sealed class NotificationApplicationServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc);

    private static Reminder DueReminder() =>
        new MedicationReminderFactory().CreateReminder(
            new ReminderCreationContext(100, 50, Now, "Paracetamol", "500 mg"));

    private static (NotificationApplicationService service,
                    InMemoryReminderRepository reminders,
                    ImmediateDelayProvider delay)
        Build(FakeNotificationSender sender, NotificationPreference? preference = null)
    {
        var reminders = new InMemoryReminderRepository();
        var preferences = new InMemoryNotificationPreferenceRepository();
        if (preference is not null)
            preferences.Seed(preference);

        var delay = new ImmediateDelayProvider();
        var options = Options.Create(new ReminderNotificationOptions
        {
            MaxAttempts = 3,
            BaseDelaySeconds = 1,
            EnableLocalFallback = true
        });

        var service = new NotificationApplicationService(
            reminders, preferences, sender, new FakeUnitOfWork(),
            new FakeClock(Now), delay, options, NullLogger<NotificationApplicationService>.Instance);

        return (service, reminders, delay);
    }

    [Fact]
    public async Task Dispatch_delivers_on_first_attempt()
    {
        var sender = FakeNotificationSender.AlwaysSucceeds();
        var (service, _, _) = Build(sender);
        var reminder = DueReminder();

        var delivered = await service.DispatchAsync(reminder);

        delivered.Should().BeTrue();
        sender.Attempts.Should().Be(1);
        reminder.Status.Should().Be(ReminderStatus.Sent);
    }

    [Fact]
    public async Task Dispatch_retries_with_backoff_then_falls_back_to_local()
    {
        var sender = FakeNotificationSender.AlwaysFails();
        var (service, _, delay) = Build(sender);
        var reminder = DueReminder();

        var delivered = await service.DispatchAsync(reminder);

        delivered.Should().BeTrue(); // entregado por fallback local (AC-09)
        sender.Attempts.Should().Be(3); // agotó MaxAttempts en push
        delay.DelayCount.Should().Be(2); // backoff entre los 3 intentos
        reminder.NotificationLogs.Should().Contain(l => l.Channel == NotificationChannel.Local);
    }

    [Fact]
    public async Task Dispatch_skips_when_global_notifications_disabled()
    {
        var preference = NotificationPreference.Default(100);
        preference.Update(soundEnabled: true, vibrationEnabled: true, repeatCount: 1, globalEnabled: false);
        var sender = FakeNotificationSender.AlwaysSucceeds();
        var (service, _, _) = Build(sender, preference);
        var reminder = DueReminder();

        var delivered = await service.DispatchAsync(reminder);

        delivered.Should().BeFalse();
        sender.Attempts.Should().Be(0); // nunca se intenta enviar
    }
}
