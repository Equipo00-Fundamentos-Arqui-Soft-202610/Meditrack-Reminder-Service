using MediTrack.ReminderService.Domain.Entities;

namespace MediTrack.ReminderService.Application.Dtos;

/// <summary>Vista de lectura de un recordatorio expuesta por la API.</summary>
public sealed record ReminderDto(
    long Id,
    long PatientId,
    string EntityType,
    long EntityId,
    DateTime ScheduledAt,
    string Status,
    string Title,
    string Body,
    DateTime? CancelledAt)
{
    public static ReminderDto FromEntity(Reminder reminder) => new(
        reminder.Id,
        reminder.PatientId,
        reminder.EntityType.ToString(),
        reminder.EntityId,
        reminder.ScheduledAt,
        reminder.Status.ToString(),
        reminder.Title,
        reminder.Body,
        reminder.CancelledAt);
}
