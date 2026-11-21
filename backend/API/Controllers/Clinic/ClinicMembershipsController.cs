using Application.DTOs.ClinicMembership;
using Application.Interfaces.Clinic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Controllers.Clinic
{
    [ApiController]
    [Route("api/clinics/{clinicId:guid}/members")]
    [Authorize]
    public sealed class ClinicMembershipsController : ApiControllerBase
    {
        private readonly IClinicMembershipService _clinicMembershipService;
        public ClinicMembershipsController(IClinicMembershipService clinicMembershipService)
        {
            _clinicMembershipService = clinicMembershipService;

        }
        [HttpGet]
        [ProducesResponseType(
            typeof(ApiResponse<List<ClinicMembershipResponseDto>>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ApiResponse<List<ClinicMembershipResponseDto>>),
            StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<List<ClinicMembershipResponseDto>>>> GetMembers(
            [FromRoute(Name = "clinicId")] Guid clinicId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<List<ClinicMembershipResponseDto>>.Error
                    (StatusCodes.Status401Unauthorized,
                    "User not authenticated"));
            }
            var result = await _clinicMembershipService.GetClinicMembershipsAsync(currentUserId,clinicId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{memberUserId:guid}")]
        [ProducesResponseType(
            typeof(ApiResponse<ClinicMembershipResponseDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ApiResponse<ClinicMembershipResponseDto>),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            typeof(ApiResponse<ClinicMembershipResponseDto>),
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ClinicMembershipResponseDto>>> GetMember(
            [FromRoute] Guid clinicId,
            [FromRoute] Guid memberUserId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<List<ClinicMembershipResponseDto>>.Error
                    (StatusCodes.Status401Unauthorized,
                    "User not authenticated"));
            }

            var result = await _clinicMembershipService.GetClinicMembershipByUserIdAsync(
                currentUserId, 
                clinicId, 
                memberUserId);
            return StatusCode(result.StatusCode, result);

        }

        [HttpPost]
        [ProducesResponseType(
            typeof(ApiResponse<ClinicMembershipResponseDto>),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            typeof(ApiResponse<ClinicMembershipResponseDto>),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            typeof(ApiResponse<ClinicMembershipResponseDto>),
            StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<ClinicMembershipResponseDto>>> CreateMembership(
            [FromRoute] Guid clinicId, 
            [FromBody] AddClinicMembershipRequestDto request)
        {
            if(!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<List<ClinicMembershipResponseDto>>.Error
                    (StatusCodes.Status401Unauthorized,
                    "User not authenticated"));
            }

            var result = await _clinicMembershipService.CreateClinicMembershipAsync(
                currentUserId, 
                clinicId, 
                request);

            return StatusCode(result.StatusCode, result);
        }
        [HttpPut("{memberUserId:guid}")]
        [ProducesResponseType(
            typeof(ApiResponse<ClinicMembershipResponseDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ApiResponse<ClinicMembershipResponseDto>),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            typeof(ApiResponse<ClinicMembershipResponseDto>),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            typeof(ApiResponse<ClinicMembershipResponseDto>),
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            typeof(ApiResponse<ClinicMembershipResponseDto>),
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ClinicMembershipResponseDto>>> UpdateMembership(
            [FromRoute] Guid clinicId, 
            [FromRoute] Guid memberUserId,
            [FromBody] UpdateClinicMembershipRoleRequestDto request)
        {
            if(!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<List<ClinicMembershipResponseDto>>.Error
                    (StatusCodes.Status401Unauthorized,
                    "User not authenticated"));
            }

            var result = await _clinicMembershipService.UpdateClinicMembershipAsync(currentUserId, clinicId, memberUserId,request);

            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{memberUserId:guid}")]
        [ProducesResponseType(
            typeof(ApiResponse<bool>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ApiResponse<bool>),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            typeof(ApiResponse<bool>),
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            typeof(ApiResponse<bool>),
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteMembership(
            [FromRoute] Guid clinicId, 
            [FromRoute] Guid memberUserId)
        {
            if(!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(
                    ApiResponse<bool>.Error
                    (StatusCodes.Status401Unauthorized,
                    "User not authenticated"));
            }

            var result = await _clinicMembershipService.DeleteClinicMembershipAsync(
                currentUserId, 
                clinicId, 
                memberUserId);

            return StatusCode(result.StatusCode, result);
        }
    }
}
