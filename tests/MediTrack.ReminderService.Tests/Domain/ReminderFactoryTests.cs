using FluentAssertions;
using MediTrack.ReminderService.Domain.Enums;
using MediTrack.ReminderService.Domain.Factories;
using Xunit;

namespace MediTrack.ReminderService.Tests.Domain;

public sealed class ReminderFactoryTests
{
    private static readonly DateTime Event = new(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void MedicationFactory_schedules_at_dose_time_and_mentions_medication()
    {
        var factory = new MedicationReminderFactory();
        var context = new ReminderCreationContext(PatientId: 1, EntityId: 50, Event, "Paracetamol", "500 mg");

        var reminder = factory.CreateReminder(context);

        reminder.EntityType.Should().Be(ReminderEntityType.Medication);
        reminder.ScheduledAt.Should().Be(Event); // sin anticipación
        reminder.Body.Should().Contain("Paracetamol").And.Contain("500 mg");
        reminder.Status.Should().Be(ReminderStatus.Scheduled);
    }

    [Fact]
    public void AppointmentFactory_schedules_24_hours_before()
    {
        var factory = new AppointmentReminderFactory(
            TimeSpan.FromHours(24), AppointmentReminderFactory.TwentyFourHourBody);
        var context = new ReminderCreationContext(1, 70, Event, "Cardiología", "Sede San Borja");

        var reminder = factory.CreateReminder(context);

        reminder.EntityType.Should().Be(ReminderEntityType.Appointment);
        reminder.ScheduledAt.Should().Be(Event.AddHours(-24));
        reminder.Body.Should().Contain("Cardiología");
    }

    [Fact]
    public void ExamFactory_schedules_2_hours_before()
    {
        var factory = new ExamReminderFactory();
        var context = new ReminderCreationContext(1, 90, Event, "Hemograma completo");

        var reminder = factory.CreateReminder(context);

        reminder.EntityType.Should().Be(ReminderEntityType.Exam);
        reminder.ScheduledAt.Should().Be(Event.AddHours(-2));
        reminder.Body.Should().Contain("Hemograma");
    }

    [Fact]
    public void Provider_returns_the_specialized_factory_for_each_type()
    {
        var provider = new ReminderFactoryProvider(new ReminderFactory[]
        {
            new MedicationReminderFactory(),
            new AppointmentReminderFactory(
                TimeSpan.FromHours(24), AppointmentReminderFactory.TwentyFourHourBody),
            new ExamReminderFactory()
        });

        provider.For(ReminderEntityType.Medication).Should().BeOfType<MedicationReminderFactory>();
        provider.For(ReminderEntityType.Appointment).Should().BeOfType<AppointmentReminderFactory>();
        provider.For(ReminderEntityType.Exam).Should().BeOfType<ExamReminderFactory>();
    }
}
