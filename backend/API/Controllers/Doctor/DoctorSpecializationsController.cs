using Application.DTOs.Doctor.DoctorSpecialization;
using Application.Interfaces.Doctor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Controllers.Doctor;

[ApiController]
[Route("api/doctors/{doctorUserId:guid}/specializations")]
[Authorize]
public sealed class DoctorSpecializationsController : ApiControllerBase
{
    private readonly IDoctorSpecializationService _doctorSpecializationService;

    public DoctorSpecializationsController(
        IDoctorSpecializationService doctorSpecializationService)
    {
        _doctorSpecializationService = doctorSpecializationService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<List<DoctorSpecializationResponseDto>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<List<DoctorSpecializationResponseDto>>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<List<DoctorSpecializationResponseDto>>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<DoctorSpecializationResponseDto>>>> GetDoctorSpecializations(
        [FromRoute] Guid doctorUserId)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(
                ApiResponse<List<DoctorSpecializationResponseDto>>.Error(
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized"));
        }

        var result = await _doctorSpecializationService
            .GetDoctorSpecializationsAsync(currentUserId, doctorUserId);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<DoctorSpecializationResponseDto>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiResponse<DoctorSpecializationResponseDto>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<DoctorSpecializationResponseDto>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<DoctorSpecializationResponseDto>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DoctorSpecializationResponseDto>>> CreateDoctorSpecialization(
        [FromRoute] Guid doctorUserId,
        [FromBody] AddDoctorSpecializationRequestDto request)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(
                ApiResponse<DoctorSpecializationResponseDto>.Error(
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized"));
        }

        var result = await _doctorSpecializationService
            .AddDoctorSpecializationAsync(currentUserId, doctorUserId, request);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{doctorSpecializationId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<DoctorSpecializationResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<DoctorSpecializationResponseDto>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<DoctorSpecializationResponseDto>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<DoctorSpecializationResponseDto>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DoctorSpecializationResponseDto>>> UpdateDoctorSpecialization(
        [FromRoute] Guid doctorUserId,
        [FromRoute] Guid doctorSpecializationId,
        [FromBody] UpdateDoctorSpecializationRequestDto request)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(
                ApiResponse<DoctorSpecializationResponseDto>.Error(
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized"));
        }

        var result = await _doctorSpecializationService
            .UpdateDoctorSpecializationAsync(
                currentUserId,
                doctorUserId,
                doctorSpecializationId,
                request);

        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{doctorSpecializationId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteDoctorSpecialization(
        [FromRoute] Guid doctorUserId,
        [FromRoute] Guid doctorSpecializationId)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(
                ApiResponse<bool>.Error(
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized"));
        }

        var result = await _doctorSpecializationService
            .DeleteDoctorSpecializationAsync(
                currentUserId,
                doctorUserId,
                doctorSpecializationId);

        return StatusCode(result.StatusCode, result);
    }
}
