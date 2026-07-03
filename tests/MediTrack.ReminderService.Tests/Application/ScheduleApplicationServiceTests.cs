using FluentAssertions;
using MediTrack.ReminderService.Application.IntegrationEvents;
using MediTrack.ReminderService.Application.Services;
using MediTrack.ReminderService.Domain.Enums;
using MediTrack.ReminderService.Domain.Factories;
using MediTrack.ReminderService.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MediTrack.ReminderService.Tests.Application;

public sealed class ScheduleApplicationServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc);

    private static (ScheduleApplicationService service, InMemoryReminderRepository reminders) Build()
    {
        var reminders = new InMemoryReminderRepository();
        var provider = new ReminderFactoryProvider(new ReminderFactory[]
        {
            new MedicationReminderFactory(),
            new AppointmentReminderFactory(
                TimeSpan.FromHours(24), AppointmentReminderFactory.TwentyFourHourBody),
            new ExamReminderFactory()
        });
        var service = new ScheduleApplicationService(
            reminders, provider, new FakeUnitOfWork(), new FakeClock(Now),
            NullLogger<ScheduleApplicationService>.Instance,
            new AppointmentReminderFactory(
                TimeSpan.FromHours(2), AppointmentReminderFactory.TwoHourBody));
        return (service, reminders);
    }

    [Fact]
    public async Task HandleRecetaCargada_creates_one_reminder_per_medication()
    {
        var (service, reminders) = Build();
        var @event = new RecetaCargadaEvent
        {
            PatientId = 100,
            PrescriptionId = 1,
            Medications = new[]
            {
                new MedicationScheduleItem(1, "Paracetamol", "500 mg", Now.AddHours(1)),
                new MedicationScheduleItem(2, "Ibuprofeno", "400 mg", Now.AddHours(2))
            }
        };

        await service.HandleRecetaCargadaAsync(@event);

        reminders.Store.Should().HaveCount(2);
        reminders.Store.Should().OnlyContain(r => r.EntityType == ReminderEntityType.Medication);
    }

    [Fact]
    public async Task HandleCumplimientoRegistrado_cancels_pending_reminders_of_entity()
    {
        var (service, reminders) = Build();
        await service.HandleRecetaCargadaAsync(new RecetaCargadaEvent
        {
            PatientId = 100,
            PrescriptionId = 1,
            Medications = new[] { new MedicationScheduleItem(1, "Paracetamol", "500 mg", Now.AddHours(1)) }
        });

        await service.HandleCumplimientoRegistradoAsync(new CumplimientoRegistradoEvent
        {
            PatientId = 100,
            EntityType = ReminderEntityType.Medication,
            EntityId = 1,
            OccurrenceDateUtc = Now.AddHours(1)
        });

        reminders.Store.Should().ContainSingle()
            .Which.Status.Should().Be(ReminderStatus.Cancelled);
    }

    [Fact]
    public async Task HandleCumplimientoRegistrado_cancels_only_todays_reminder()
    {
        var (service, reminders) = Build();
        var limaTz = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

        // Day 1 at 08:00 Lima = 13:00 UTC
        var day1 = TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Unspecified), limaTz);
        // Day 2 at 08:00 Lima = 13:00 UTC
        var day2 = TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(2026, 6, 11, 8, 0, 0, DateTimeKind.Unspecified), limaTz);
        // Day 3 at 08:00 Lima = 13:00 UTC
        var day3 = TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(2026, 6, 12, 8, 0, 0, DateTimeKind.Unspecified), limaTz);

        await service.HandleRecetaCargadaAsync(new RecetaCargadaEvent
        {
            PatientId = 100,
            PrescriptionId = 1,
            Medications = new[]
            {
                new MedicationScheduleItem(1, "Paracetamol", "500 mg", day1),
                new MedicationScheduleItem(1, "Paracetamol", "500 mg", day2),
                new MedicationScheduleItem(1, "Paracetamol", "500 mg", day3)
            }
        });

        // Cancel only Day 2
        await service.HandleCumplimientoRegistradoAsync(new CumplimientoRegistradoEvent
        {
            PatientId = 100,
            EntityType = ReminderEntityType.Medication,
            EntityId = 1,
            OccurrenceDateUtc = day2
        });

        reminders.Store.Count(r => r.Status == ReminderStatus.Scheduled).Should().Be(2);
        reminders.Store.Count(r => r.Status == ReminderStatus.Cancelled).Should().Be(1);
    }

    [Fact]
    public async Task CancelReminder_returns_false_when_not_found()
    {
        var (service, _) = Build();

        var result = await service.CancelReminderAsync(reminderId: 999);

        result.Should().BeFalse();
    }
}
