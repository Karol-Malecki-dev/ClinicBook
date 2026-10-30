using Application.DTOs.Clinic;
using Application.Interfaces.Clinic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Controllers.Clinic;

[ApiController]
[Route("api/clinics/{clinicId:guid}")]
[Authorize]
public sealed class DoctorAvailabilityController : ApiControllerBase
{
    private readonly IDoctorAvailabilityService _availabilityService;

    public DoctorAvailabilityController(IDoctorAvailabilityService availabilityService)
    {
        ArgumentNullException.ThrowIfNull(availabilityService);
        _availabilityService = availabilityService;
    }

    [HttpGet("doctors")]
    public async Task<ActionResult<ApiResponse<List<ClinicDoctorDto>>>> GetDoctors(
        [FromRoute] Guid clinicId)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<List<ClinicDoctorDto>>.Error(401, "User not authenticated."));
        }

        var result = await _availabilityService.GetClinicDoctorsAsync(currentUserId, clinicId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("doctors/{doctorUserId:guid}/availability")]
    public async Task<ActionResult<ApiResponse<List<DoctorWeeklyAvailabilityDto>>>> GetWeeklyAvailability(
        [FromRoute] Guid clinicId,
        [FromRoute] Guid doctorUserId)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<List<DoctorWeeklyAvailabilityDto>>.Error(401, "User not authenticated."));
        }

        var result = await _availabilityService.GetWeeklyAvailabilityAsync(
            currentUserId,
            clinicId,
            doctorUserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("doctors/{doctorUserId:guid}/availability/slots")]
    public async Task<ActionResult<ApiResponse<List<AvailabilitySlotDto>>>> GetAvailableSlots(
        [FromRoute] Guid clinicId,
        [FromRoute] Guid doctorUserId,
        [FromQuery] DateOnly? date)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<List<AvailabilitySlotDto>>.Error(401, "User not authenticated."));
        }

        if (!date.HasValue)
        {
            return BadRequest(ApiResponse<List<AvailabilitySlotDto>>.Error(400, "The clinic-local date is required."));
        }

        var result = await _availabilityService.GetAvailableSlotsAsync(
            currentUserId,
            clinicId,
            doctorUserId,
            date.Value);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("doctors/{doctorUserId:guid}/availability")]
    public async Task<ActionResult<ApiResponse<DoctorWeeklyAvailabilityDto>>> CreateWeeklyAvailability(
        [FromRoute] Guid clinicId,
        [FromRoute] Guid doctorUserId,
        [FromBody] SetDoctorWeeklyAvailabilityRequestDto request)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<DoctorWeeklyAvailabilityDto>.Error(401, "User not authenticated."));
        }

        var result = await _availabilityService.CreateWeeklyAvailabilityAsync(
            currentUserId,
            clinicId,
            doctorUserId,
            request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("doctors/{doctorUserId:guid}/availability/{availabilityId:guid}")]
    public async Task<ActionResult<ApiResponse<DoctorWeeklyAvailabilityDto>>> UpdateWeeklyAvailability(
        [FromRoute] Guid clinicId,
        [FromRoute] Guid doctorUserId,
        [FromRoute] Guid availabilityId,
        [FromBody] SetDoctorWeeklyAvailabilityRequestDto request)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<DoctorWeeklyAvailabilityDto>.Error(401, "User not authenticated."));
        }

        var result = await _availabilityService.UpdateWeeklyAvailabilityAsync(
            currentUserId,
            clinicId,
            doctorUserId,
            availabilityId,
            request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("doctors/{doctorUserId:guid}/availability/{availabilityId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteWeeklyAvailability(
        [FromRoute] Guid clinicId,
        [FromRoute] Guid doctorUserId,
        [FromRoute] Guid availabilityId)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<bool>.Error(401, "User not authenticated."));
        }

        var result = await _availabilityService.DeleteWeeklyAvailabilityAsync(
            currentUserId,
            clinicId,
            doctorUserId,
            availabilityId);
        return StatusCode(result.StatusCode, result);
    }
}
