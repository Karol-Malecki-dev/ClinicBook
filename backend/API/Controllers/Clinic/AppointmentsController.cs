using Application.DTOs.Clinic;
using Application.Interfaces.Clinic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Controllers.Clinic;

[ApiController]
[Route("api/appointments")]
[Authorize]
public sealed class AppointmentsController : ApiControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        ArgumentNullException.ThrowIfNull(appointmentService);
        _appointmentService = appointmentService;
    }

    [HttpGet("mine")]
    public async Task<ActionResult<ApiResponse<List<AppointmentResponseDto>>>> GetMyAppointments()
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<List<AppointmentResponseDto>>.Error(401, "User not authenticated."));
        }

        var result = await _appointmentService.GetPatientAppointmentsAsync(currentUserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("~/api/clinics/{clinicId:guid}/doctors/{doctorUserId:guid}/appointments")]
    public async Task<ActionResult<ApiResponse<AppointmentResponseDto>>> BookAppointment(
        [FromRoute] Guid clinicId,
        [FromRoute] Guid doctorUserId,
        [FromBody] CreateAppointmentRequestDto request)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<AppointmentResponseDto>.Error(401, "User not authenticated."));
        }

        var result = await _appointmentService.BookAppointmentAsync(
            currentUserId,
            clinicId,
            doctorUserId,
            request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("~/api/clinics/{clinicId:guid}/appointments")]
    public async Task<ActionResult<ApiResponse<List<AppointmentResponseDto>>>> GetClinicAppointments(
        [FromRoute] Guid clinicId,
        [FromQuery] DateOnly? date)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<List<AppointmentResponseDto>>.Error(401, "User not authenticated."));
        }

        if (!date.HasValue)
        {
            return BadRequest(
                ApiResponse<List<AppointmentResponseDto>>.Error(400, "A clinic-local date is required."));
        }

        var result = await _appointmentService.GetClinicAppointmentsAsync(
            currentUserId,
            clinicId,
            date.Value);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{appointmentId:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<AppointmentResponseDto>>> CancelAppointment(
        [FromRoute] Guid appointmentId)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<AppointmentResponseDto>.Error(401, "User not authenticated."));
        }

        var result = await _appointmentService.CancelAppointmentAsync(currentUserId, appointmentId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{appointmentId:guid}/complete")]
    public async Task<ActionResult<ApiResponse<AppointmentResponseDto>>> CompleteAppointment(
        [FromRoute] Guid appointmentId)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<AppointmentResponseDto>.Error(401, "User not authenticated."));
        }

        var result = await _appointmentService.CompleteAppointmentAsync(currentUserId, appointmentId);
        return StatusCode(result.StatusCode, result);
    }
}
