using Application.DTOs.Doctor.DoctorProfile;
using Shared.Responses;

namespace Application.Interfaces.Doctor;

/// <summary>
/// Provides operations for doctor profiles.
/// </summary>
public interface IDoctorProfileService
{
    Task<ApiResponse<DoctorProfileResponseDto>> GetDoctorProfileAsync(
        Guid currentUserId,
        Guid doctorUserId);

    Task<ApiResponse<DoctorProfileResponseDto>> CreateDoctorProfileAsync(
        Guid currentUserId,
        Guid doctorUserId,
        CreateDoctorProfileRequestDto request);

    Task<ApiResponse<DoctorProfileResponseDto>> UpdateDoctorProfileAsync(
        Guid currentUserId,
        Guid doctorUserId,
        UpdateDoctorProfileRequestDto request);

    Task<ApiResponse<bool>> DeleteDoctorProfileAsync(
        Guid currentUserId,
        Guid doctorUserId);
}