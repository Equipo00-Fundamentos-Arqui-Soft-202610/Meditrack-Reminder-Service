using FluentAssertions;
using MediTrack.ReminderService.Domain.Enums;
using MediTrack.ReminderService.Domain.Factories;
using Xunit;

namespace MediTrack.ReminderService.Tests.Domain;

public sealed class ReminderTests
{
    private static readonly DateTime Now = new(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc);

    private static MediTrack.ReminderService.Domain.Entities.Reminder NewMedicationReminder() =>
        new MedicationReminderFactory().CreateReminder(
            new ReminderCreationContext(1, 50, Now, "Paracetamol", "500 mg"));

    [Fact]
    public void Cancel_marks_status_and_timestamp()
    {
        var reminder = NewMedicationReminder();

        reminder.Cancel(Now);

        reminder.Status.Should().Be(ReminderStatus.Cancelled);
        reminder.CancelledAt.Should().Be(Now);
    }

    [Fact]
    public void Cancel_is_idempotent_and_does_not_override_sent()
    {
        var reminder = NewMedicationReminder();
        reminder.RegisterDeliveryAttempt(NotificationChannel.Push, DeliveryStatus.Delivered, Now);

        reminder.Cancel(Now.AddMinutes(5));

        reminder.Status.Should().Be(ReminderStatus.Sent); // no se sobreescribe un envío confirmado
    }

    [Fact]
    public void RegisterDeliveryAttempt_delivered_transitions_to_sent_and_logs()
    {
        var reminder = NewMedicationReminder();

        reminder.RegisterDeliveryAttempt(NotificationChannel.Push, DeliveryStatus.Delivered, Now);

        reminder.Status.Should().Be(ReminderStatus.Sent);
        reminder.NotificationLogs.Should().ContainSingle();
    }

    [Fact]
    public void IsDue_is_true_only_for_scheduled_and_past()
    {
        var reminder = NewMedicationReminder();

        reminder.IsDue(Now).Should().BeTrue();
        reminder.IsDue(Now.AddMinutes(-1)).Should().BeFalse();
    }
}
