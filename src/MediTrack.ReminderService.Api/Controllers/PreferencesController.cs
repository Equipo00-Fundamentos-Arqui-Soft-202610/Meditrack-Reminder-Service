using MediTrack.ReminderService.Application.Dtos;
using MediTrack.ReminderService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediTrack.ReminderService.Api.Controllers;

/// <summary>
/// "Preference Controller" (Fig. 17): expone los endpoints para consultar y
/// configurar las preferencias de notificación del paciente (US22).
/// </summary>
[ApiController]
[Authorize]
[Route("reminders/preferences")]
[Produces("application/json")]
public sealed class PreferencesController : ControllerBase
{
    private readonly PreferenceApplicationService _preferences;

    public PreferencesController(PreferenceApplicationService preferences) => _preferences = preferences;

    /// <summary>Obtiene las preferencias de notificación del paciente (o las de por defecto).</summary>
    /// <param name="patientId">Identificador del paciente.</param>
    /// <response code="200">Preferencias del paciente.</response>
    [HttpGet("patients/{patientId:long}")]
    [ProducesResponseType(typeof(NotificationPreferenceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationPreferenceDto>> Get(long patientId, CancellationToken cancellationToken)
    {
        var preference = await _preferences.GetAsync(patientId, cancellationToken);
        return Ok(preference);
    }

    /// <summary>Crea o actualiza las preferencias de notificación del paciente.</summary>
    /// <param name="patientId">Identificador del paciente.</param>
    /// <param name="request">Nuevas preferencias.</param>
    /// <response code="200">Preferencias guardadas.</response>
    /// <response code="400">Datos inválidos (p. ej. repeticiones menores a 1).</response>
    [HttpPut("patients/{patientId:long}")]
    [ProducesResponseType(typeof(NotificationPreferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NotificationPreferenceDto>> Upsert(
        long patientId, [FromBody] UpdateNotificationPreferenceRequest request, CancellationToken cancellationToken)
    {
        if (request.RepeatCount < 1)
            return BadRequest("Las repeticiones deben ser al menos 1.");

        var preference = await _preferences.UpsertAsync(patientId, request, cancellationToken);
        return Ok(preference);
    }
}
