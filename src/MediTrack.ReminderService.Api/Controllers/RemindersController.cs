using MediTrack.ReminderService.Application.Dtos;
using MediTrack.ReminderService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediTrack.ReminderService.Api.Controllers;

/// <summary>
/// "Reminder Controller" (Fig. 17): expone los endpoints para consultar los
/// recordatorios activos de un paciente y cancelar un recordatorio.
/// </summary>
[ApiController]
[Authorize]
[Route("reminders")]
[Produces("application/json")]
public sealed class RemindersController : ControllerBase
{
    private readonly ScheduleApplicationService _schedule;

    public RemindersController(ScheduleApplicationService schedule) => _schedule = schedule;

    /// <summary>Lista los recordatorios activos (programados) de un paciente.</summary>
    /// <param name="patientId">Identificador del paciente.</param>
    /// <response code="200">Lista de recordatorios activos.</response>
    [HttpGet("patients/{patientId:long}")]
    [ProducesResponseType(typeof(IReadOnlyList<ReminderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReminderDto>>> GetByPatient(
        long patientId, CancellationToken cancellationToken)
    {
        var reminders = await _schedule.GetActiveByPatientAsync(patientId, cancellationToken);
        return Ok(reminders);
    }

    /// <summary>Cancela un recordatorio programado (p. ej. tras cumplir la dosis).</summary>
    /// <param name="id">Identificador del recordatorio.</param>
    /// <response code="204">Recordatorio cancelado.</response>
    /// <response code="404">No existe un recordatorio con ese identificador.</response>
    [HttpPut("{id:long}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(long id, CancellationToken cancellationToken)
    {
        var cancelled = await _schedule.CancelReminderAsync(id, cancellationToken);
        return cancelled ? NoContent() : NotFound();
    }
}
