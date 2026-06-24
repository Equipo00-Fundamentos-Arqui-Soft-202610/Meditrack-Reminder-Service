using MediTrack.ReminderService.Domain.Enums;

namespace MediTrack.ReminderService.Application.Abstractions;

/// <summary>
/// Mensaje listo para entregar a través de un canal de notificación. El destino
/// es un "topic" de FCM por paciente (<c>patient_{id}</c>), de modo que el Reminder
/// Service no necesita almacenar tokens de dispositivo (desacoplamiento).
/// </summary>
public sealed record NotificationMessage(
    int PatientId,
    string Topic,
    string Title,
    string Body,
    bool SoundEnabled,
    bool VibrationEnabled,
    NotificationChannel Channel);
