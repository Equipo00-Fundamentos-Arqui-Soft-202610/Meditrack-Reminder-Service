using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using MediTrack.ReminderService.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediTrack.ReminderService.Infrastructure.Notifications;

/// <summary>
/// "Notification Adapter" (Fig. 17): puerto de salida hacia Firebase Cloud
/// Messaging (CON-05). Envía la notificación a un topic por paciente. Si no hay
/// credenciales configuradas, opera en modo simulado para no bloquear el desarrollo.
/// </summary>
public sealed class FcmNotificationSender : INotificationSender, IDisposable
{
    private readonly ILogger<FcmNotificationSender> _logger;
    private readonly bool _simulate;
    private readonly FirebaseApp? _firebaseApp;
    private readonly FirebaseMessaging? _messaging;

    public FcmNotificationSender(IOptions<FcmOptions> options, ILogger<FcmNotificationSender> logger)
    {
        _logger = logger;
        var config = options.Value;

        var hasCredentials = !string.IsNullOrWhiteSpace(config.CredentialsPath) && File.Exists(config.CredentialsPath);
        _simulate = config.SimulateDelivery || !hasCredentials;

        if (_simulate)
        {
            _logger.LogWarning(
                "FcmNotificationSender en modo SIMULADO (sin credenciales válidas de FCM). No se envían notificaciones reales.");
            return;
        }

        _firebaseApp = FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromFile(config.CredentialsPath),
            ProjectId = config.ProjectId
        });
        _messaging = FirebaseMessaging.GetMessaging(_firebaseApp);
    }

    public async Task<NotificationDeliveryResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (_simulate || _messaging is null)
        {
            _logger.LogInformation(
                "[SIMULADO] Push a topic {Topic}: \"{Title} — {Body}\".", message.Topic, message.Title, message.Body);
            return NotificationDeliveryResult.Ok("simulated");
        }

        try
        {
            var fcmMessage = new Message
            {
                Topic = message.Topic,
                Notification = new Notification { Title = message.Title, Body = message.Body },
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        DefaultSound = message.SoundEnabled,
                        DefaultVibrateTimings = message.VibrationEnabled
                    }
                }
            };

            var providerMessageId = await _messaging.SendAsync(fcmMessage, cancellationToken);
            return NotificationDeliveryResult.Ok(providerMessageId);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Error de FCM al enviar al topic {Topic}.", message.Topic);
            return NotificationDeliveryResult.Fail(ex.Message);
        }
    }

    public void Dispose() => _firebaseApp?.Delete();
}
