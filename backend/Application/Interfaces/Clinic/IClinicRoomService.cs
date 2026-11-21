using Application.DTOs.ClinicRoom;
using Shared.Responses;

namespace Application.Interfaces.Clinic;

/// <summary>
/// Provides operations for rooms belonging to a clinic.
/// </summary>
public interface IClinicRoomService
{
    Task<ApiResponse<List<ClinicRoomResponseDto>>> GetClinicRoomsAsync(
        Guid currentUserId,
        Guid clinicId);

    Task<ApiResponse<ClinicRoomResponseDto>> GetClinicRoomByIdAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid roomId);

    Task<ApiResponse<ClinicRoomResponseDto>> CreateClinicRoomAsync(
        Guid currentUserId,
        Guid clinicId,
        CreateClinicRoomRequestDto request);

    Task<ApiResponse<ClinicRoomResponseDto>> UpdateClinicRoomAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid roomId,
        UpdateClinicRoomRequestDto request);

    Task<ApiResponse<bool>> DeleteClinicRoomAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid roomId);
}
