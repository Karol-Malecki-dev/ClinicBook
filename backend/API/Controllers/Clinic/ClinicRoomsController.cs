using Application.DTOs.ClinicRoom;
using Application.Interfaces.Clinic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Controllers.Clinic;

[ApiController]
[Route("api/clinics/{clinicId:guid}/rooms")]
[Authorize]
public sealed class ClinicRoomsController : ApiControllerBase
{
    private readonly IClinicRoomService _clinicRoomService;

    public ClinicRoomsController(IClinicRoomService clinicRoomService)
    {
        _clinicRoomService = clinicRoomService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ClinicRoomResponseDto>>>> GetRooms(
        [FromRoute] Guid clinicId)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(
                ApiResponse<List<ClinicRoomResponseDto>>.Error(
                    StatusCodes.Status401Unauthorized,
                    "User not authenticated"));
        }

        var result = await _clinicRoomService.GetClinicRoomsAsync(
            currentUserId,
            clinicId);

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{roomId:guid}")]
    public async Task<ActionResult<ApiResponse<ClinicRoomResponseDto>>> GetRoom(
        [FromRoute] Guid clinicId,
        [FromRoute] Guid roomId)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(
                ApiResponse<ClinicRoomResponseDto>.Error(
                    StatusCodes.Status401Unauthorized,
                    "User not authenticated"));
        }

        var result = await _clinicRoomService.GetClinicRoomByIdAsync(
            currentUserId,
            clinicId,
            roomId);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ClinicRoomResponseDto>>> Create(
        [FromRoute] Guid clinicId,
        [FromBody] CreateClinicRoomRequestDto request)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(
                ApiResponse<ClinicRoomResponseDto>.Error(
                    StatusCodes.Status401Unauthorized,
                    "User not authenticated"));
        }

        var result = await _clinicRoomService.CreateClinicRoomAsync(
            currentUserId,
            clinicId,
            request);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{roomId:guid}")]
    public async Task<ActionResult<ApiResponse<ClinicRoomResponseDto>>> Update(
        [FromRoute] Guid clinicId,
        [FromRoute] Guid roomId,
        [FromBody] UpdateClinicRoomRequestDto request)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(
                ApiResponse<ClinicRoomResponseDto>.Error(
                    StatusCodes.Status401Unauthorized,
                    "User not authenticated"));
        }

        var result = await _clinicRoomService.UpdateClinicRoomAsync(
            currentUserId,
            clinicId,
            roomId,
            request);

        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{roomId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        [FromRoute] Guid clinicId,
        [FromRoute] Guid roomId)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(
                ApiResponse<bool>.Error(
                    StatusCodes.Status401Unauthorized,
                    "User not authenticated"));
        }

        var result = await _clinicRoomService.DeleteClinicRoomAsync(
            currentUserId,
            clinicId,
            roomId);

        return StatusCode(result.StatusCode, result);
    }
}
