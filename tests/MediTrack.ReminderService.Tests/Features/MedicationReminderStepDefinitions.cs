using System.Globalization;
using FluentAssertions;
using MediTrack.ReminderService.Application.IntegrationEvents;
using MediTrack.ReminderService.Application.Services;
using MediTrack.ReminderService.Domain.Enums;
using MediTrack.ReminderService.Domain.Factories;
using MediTrack.ReminderService.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Reqnroll;

namespace MediTrack.ReminderService.Tests.Features;

/// <summary>
/// Implementación de los pasos del feature BDD. Ejercita el flujo real de generación
/// y cancelación de recordatorios a través del ScheduleApplicationService.
/// </summary>
[Binding]
public sealed class MedicationReminderStepDefinitions
{
    private readonly InMemoryReminderRepository _reminders = new();
    private readonly ScheduleApplicationService _service;

    private int _patientId;
    private long _medicationId = 1;
    private string _medicationName = string.Empty;
    private string _dose = string.Empty;
    private DateTime _doseTimeUtc;

    public MedicationReminderStepDefinitions()
    {
        var provider = new ReminderFactoryProvider(new ReminderFactory[]
        {
            new MedicationReminderFactory(),
            new AppointmentReminderFactory(TimeSpan.FromHours(24), AppointmentReminderFactory.TwentyFourHourBody),
            new ExamReminderFactory()
        });
        _service = new ScheduleApplicationService(
            _reminders, provider, new FakeUnitOfWork(),
            new FakeClock(new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc)),
            NullLogger<ScheduleApplicationService>.Instance,
            new AppointmentReminderFactory(TimeSpan.FromHours(2), AppointmentReminderFactory.TwoHourBody));
    }

    [Given(@"una receta del paciente (\d+) con el medicamento ""(.*)"" dosis ""(.*)"" a las ""(.*)""")]
    public void GivenUnaReceta(int patientId, string medication, string dose, string doseTime)
    {
        _patientId = patientId;
        _medicationName = medication;
        _dose = dose;
        _doseTimeUtc = ParseUtc(doseTime);
    }

    [When(@"se procesa el evento RecetaCargada")]
    public async Task WhenSeProcesaRecetaCargada()
    {
        await _service.HandleRecetaCargadaAsync(new RecetaCargadaEvent
        {
            PatientId = _patientId,
            PrescriptionId = 1,
            Medications = new[] { new MedicationScheduleItem(_medicationId, _medicationName, _dose, _doseTimeUtc) }
        });
    }

    [When(@"se procesa el evento CumplimientoRegistrado para el medicamento (\d+) del paciente (\d+)")]
    public async Task WhenSeProcesaCumplimiento(long medicationId, int patientId)
    {
        await _service.HandleCumplimientoRegistradoAsync(new CumplimientoRegistradoEvent
        {
            PatientId = patientId,
            EntityType = ReminderEntityType.Medication,
            EntityId = medicationId
        });
    }

    [Then(@"existe (\d+) recordatorio programado para el paciente (\d+)")]
    public void ThenExisteRecordatorio(int count, int patientId)
    {
        _reminders.Store
            .Count(r => r.PatientId == patientId && r.Status == ReminderStatus.Scheduled)
            .Should().Be(count);
    }

    [Then(@"el recordatorio queda programado para las ""(.*)""")]
    public void ThenProgramadoPara(string scheduledAt)
    {
        _reminders.Store.Single().ScheduledAt.Should().Be(ParseUtc(scheduledAt));
    }

    [Then(@"el mensaje del recordatorio menciona ""(.*)""")]
    public void ThenMensajeMenciona(string text)
    {
        _reminders.Store.Single().Body.Should().Contain(text);
    }

    [Then(@"el recordatorio del paciente (\d+) queda cancelado")]
    public void ThenRecordatorioCancelado(int patientId)
    {
        _reminders.Store
            .Single(r => r.PatientId == patientId)
            .Status.Should().Be(ReminderStatus.Cancelled);
    }

    private static DateTime ParseUtc(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
}
