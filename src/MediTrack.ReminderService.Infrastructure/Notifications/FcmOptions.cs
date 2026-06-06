namespace MediTrack.ReminderService.Infrastructure.Notifications;

/// <summary>
/// Configuración del adaptador de Firebase Cloud Messaging (CON-05). Si no se
/// proporcionan credenciales, el adaptador opera en modo simulado (útil en
/// desarrollo y pruebas, sin secretos en el código).
/// </summary>
public sealed class FcmOptions
{
    public const string SectionName = "Fcm";

    /// <summary>Id del proyecto de Firebase.</summary>
    public string? ProjectId { get; set; }

    /// <summary>Ruta al archivo JSON de credenciales de la cuenta de servicio.</summary>
    public string? CredentialsPath { get; set; }

    /// <summary>
    /// Fuerza el modo simulado aunque existan credenciales. Por defecto el modo se
    /// decide según la presencia de credenciales.
    /// </summary>
    public bool SimulateDelivery { get; set; } = true;
}
