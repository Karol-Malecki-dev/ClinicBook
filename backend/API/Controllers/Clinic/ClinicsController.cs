using Application.DTOs.Clinic;
using Application.Interfaces.Clinic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Controllers.Clinic;

[ApiController]
[Route("api/clinics")]
[Authorize]
public sealed class ClinicsController : ApiControllerBase
{
    private readonly IClinicService _clinicService;

    public ClinicsController(IClinicService clinicService)
    {
        _clinicService = clinicService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<List<ClinicResponseDto>>), 
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<List<ClinicResponseDto>>),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ClinicResponseDto>>>> GetClinics()
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(
                ApiResponse<List<ClinicResponseDto>>.Error(
                    StatusCodes.Status401Unauthorized,
                    "User not authenticated"));
        }

        var result = await _clinicService.GetAccessibleClinicsAsync(currentUserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{clinicId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<ClinicResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ClinicResponseDto>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<ClinicResponseDto>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ClinicResponseDto>>> GetClinic(
        [FromRoute] Guid clinicId)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(
                ApiResponse<ClinicResponseDto>.Error(
                    StatusCodes.Status401Unauthorized,
                    "User not authenticated"));
        }

        var result = await _clinicService.GetClinicByIdAsync(
            currentUserId,
            clinicId);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<ClinicResponseDto>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiResponse<ClinicResponseDto>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<ClinicResponseDto>),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ClinicResponseDto>>> CreateClinic(
        [FromBody] CreateClinicRequestDto request)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(
                ApiResponse<ClinicResponseDto>.Error(
                    StatusCodes.Status401Unauthorized,
                    "User not authenticated"));
        }

        var result = await _clinicService.CreateClinicAsync(
            currentUserId,
            request);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{clinicId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<ClinicResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ClinicResponseDto>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<ClinicResponseDto>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<ClinicResponseDto>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiResponse<ClinicResponseDto>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ClinicResponseDto>>> UpdateClinic(
        [FromRoute] Guid clinicId,
        [FromBody] UpdateClinicRequestDto request)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(
                ApiResponse<ClinicResponseDto>.Error(
                    StatusCodes.Status401Unauthorized,
                    "User not authenticated"));
        }

        var result = await _clinicService.UpdateClinicAsync(
            currentUserId,
            clinicId,
            request);

        return StatusCode(result.StatusCode, result);
    }
}