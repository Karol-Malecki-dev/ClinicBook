using Application.DTOs.Patient;
using Application.Interfaces.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Controllers.Patient
{
    [ApiController]
    [Route("api/patients/{patientUserId:guid}/profiles")]
    [Authorize]
    public sealed class PatientProfilesController : ApiControllerBase
    {
        private readonly IPatientProfileService _patientProfileService;

        public PatientProfilesController(IPatientProfileService patientProfileService)
        {
            ArgumentNullException.ThrowIfNull(patientProfileService);
            _patientProfileService = patientProfileService;
        }

        [HttpGet]
        [ProducesResponseType
            (typeof(ApiResponse<PatientProfileResponseDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType
            (typeof(ApiResponse<PatientProfileResponseDto>),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType
            (typeof(ApiResponse<PatientProfileResponseDto>),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType
            (typeof(ApiResponse<PatientProfileResponseDto>),
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType
            (typeof(ApiResponse<PatientProfileResponseDto>),
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<PatientProfileResponseDto>>> GetPatientProfile(
            [FromRoute] Guid patientUserId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<PatientProfileResponseDto>.Error(
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized"));
            }
            var result = await _patientProfileService.GetPatientProfileAsync(currentUserId, patientUserId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [ProducesResponseType
            (typeof(ApiResponse<PatientProfileResponseDto>),
            StatusCodes.Status201Created)]
        [ProducesResponseType
            (typeof(ApiResponse<PatientProfileResponseDto>),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType
            (typeof(ApiResponse<PatientProfileResponseDto>),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType
            (typeof(ApiResponse<PatientProfileResponseDto>),
            StatusCodes.Status404NotFound)]
        [ProducesResponseType
            (typeof(ApiResponse<PatientProfileResponseDto>),
            StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ApiResponse<PatientProfileResponseDto>>> CreatePatientProfile(
            [FromRoute] Guid patientUserId,
            [FromBody] CreatePatientProfileRequestDto createDto)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<PatientProfileResponseDto>.Error(
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized"));
            }
            var result = await _patientProfileService.CreatePatientProfileAsync(currentUserId, patientUserId, createDto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut]
        [ProducesResponseType
            (typeof(ApiResponse<PatientProfileResponseDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType
            (typeof(ApiResponse<PatientProfileResponseDto>),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType
            (typeof(ApiResponse<PatientProfileResponseDto>),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType
            (typeof(ApiResponse<PatientProfileResponseDto>),
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType
            (typeof(ApiResponse<PatientProfileResponseDto>),
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<PatientProfileResponseDto>>> UpdatePatientProfile(
            [FromRoute] Guid patientUserId,
            [FromBody] UpdatePatientProfileRequestDto updateDto)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<PatientProfileResponseDto>.Error(
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized"));
            }
            var result = await _patientProfileService.UpdatePatientProfileAsync(currentUserId, patientUserId, updateDto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete]
        [ProducesResponseType
            (typeof(ApiResponse<bool>),
            StatusCodes.Status200OK)]
        [ProducesResponseType
            (typeof(ApiResponse<bool>),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType
            (typeof(ApiResponse<bool>),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType
            (typeof(ApiResponse<bool>),
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType
            (typeof(ApiResponse<bool>),
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> DeletePatientProfile(
            [FromRoute] Guid patientUserId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<bool>.Error(
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized"));
            }
            var result = await _patientProfileService.DeletePatientProfileAsync(currentUserId, patientUserId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
