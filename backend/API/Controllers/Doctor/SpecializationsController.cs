using Application.DTOs.Specialization;
using Application.Interfaces.Specialization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Controllers.Doctor
{
    public sealed class SpecializationsController : ApiControllerBase
    {
        private readonly ISpecializationService _specializationService;

        public SpecializationsController(ISpecializationService specializationService)
        {
            _specializationService = specializationService;
        }

        [HttpGet]
        [ProducesResponseType(
            typeof(ApiResponse<List<SpecializationResponseDto>>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ApiResponse<List<SpecializationResponseDto>>),
            StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<List<SpecializationResponseDto>>>> GetSpecializations()
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<List<SpecializationResponseDto>>.Error(
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized"));
            }
            var result = await _specializationService.GetSpecializationsAsync(currentUserId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        [ProducesResponseType(
            typeof(ApiResponse<SpecializationResponseDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ApiResponse<SpecializationResponseDto>),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            typeof(ApiResponse<SpecializationResponseDto>),
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<SpecializationResponseDto>>> GetSpecializationById(
            [FromRoute] Guid specializationId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<SpecializationResponseDto>.Error(
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized"));
            }
            var result = await _specializationService.GetSpecializationByIdAsync(
                currentUserId, 
                specializationId);
            
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [ProducesResponseType(
            typeof(ApiResponse<SpecializationResponseDto>),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            typeof(ApiResponse<SpecializationResponseDto>),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            typeof(ApiResponse<SpecializationResponseDto>),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            typeof(ApiResponse<SpecializationResponseDto>),
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<SpecializationResponseDto>>> CreateSpecialization(
            [FromBody] CreateSpecializationRequestDto request)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<SpecializationResponseDto>.Error(
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized"));
            }
            var result = await _specializationService.CreateSpecializationAsync(currentUserId, request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<SpecializationResponseDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SpecializationResponseDto>),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<SpecializationResponseDto>),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<SpecializationResponseDto>),
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<SpecializationResponseDto>>> UpdateSpecialization(
            [FromRoute] Guid specializationId,
            [FromBody] UpdateSpecializationRequestDto request)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<SpecializationResponseDto>.Error(
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized"));
            }
            var result = await _specializationService.UpdateSpecializationAsync(
                currentUserId,
                specializationId,
                request);

            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<bool>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<bool>),
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteSpecialization(Guid specializationId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<bool>.Error(
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized"));
            }
            var result = await _specializationService.DeleteSpecializationAsync(
                currentUserId,
                specializationId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
