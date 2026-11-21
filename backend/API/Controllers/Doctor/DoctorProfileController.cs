using Application.DTOs.Doctor.DoctorProfile;
using Application.Interfaces.Doctor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Controllers.Doctor
{
    [ApiController]
    [Route("api/doctors/{doctorUserId:guid}/profile")]
    [Authorize]

    public sealed class DoctorProfileController : ApiControllerBase
    {
        private readonly IDoctorProfileService _doctorProfileService;

        public DoctorProfileController(IDoctorProfileService doctorProfileService)
        {
            _doctorProfileService = doctorProfileService;
        }

        [HttpGet]
        [ProducesResponseType(
            typeof(ApiResponse<DoctorProfileResponseDto>),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
                typeof(ApiResponse<DoctorProfileResponseDto>),
                StatusCodes.Status200OK)]
        [ProducesResponseType(
                typeof(ApiResponse<DoctorProfileResponseDto>),
                StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<DoctorProfileResponseDto>>> GetDoctorProfile(
                [FromRoute] Guid doctorUserId
            )
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<DoctorProfileResponseDto>.Error(
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized"));
            }

            var result = await _doctorProfileService.GetDoctorProfileAsync(currentUserId, doctorUserId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [ProducesResponseType(
            typeof(ApiResponse<DoctorProfileResponseDto>),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            typeof(ApiResponse<DoctorProfileResponseDto>),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            typeof(ApiResponse<DoctorProfileResponseDto>),
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            typeof(ApiResponse<DoctorProfileResponseDto>),
            StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ApiResponse<DoctorProfileResponseDto>>> CreateDoctorProfile(
            [FromRoute] Guid doctorUserId,
            [FromBody] CreateDoctorProfileRequestDto request)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<DoctorProfileResponseDto>.Error(
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized"));
            }
            var result = await _doctorProfileService.CreateDoctorProfileAsync(currentUserId, doctorUserId, request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut]
        [ProducesResponseType(
            typeof(ApiResponse<DoctorProfileResponseDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ApiResponse<DoctorProfileResponseDto>),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            typeof(ApiResponse<DoctorProfileResponseDto>),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            typeof(ApiResponse<DoctorProfileResponseDto>),
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            typeof(ApiResponse<DoctorProfileResponseDto>),
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<DoctorProfileResponseDto>>> UpdateDoctorProfile(
            [FromRoute] Guid doctorUserId,
            [FromBody] UpdateDoctorProfileRequestDto request)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<DoctorProfileResponseDto>.Error(
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized"));
            }
            var result = await _doctorProfileService.UpdateDoctorProfileAsync(currentUserId, doctorUserId, request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete]
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
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            typeof(ApiResponse<bool>),
            StatusCodes.Status404NotFound)] 
        public async Task<ActionResult<ApiResponse<bool>>> DeleteDoctorProfile(
            [FromRoute] Guid doctorUserId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<bool>.Error(
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized"));
            }
            var result = await _doctorProfileService.DeleteDoctorProfileAsync(currentUserId, doctorUserId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
