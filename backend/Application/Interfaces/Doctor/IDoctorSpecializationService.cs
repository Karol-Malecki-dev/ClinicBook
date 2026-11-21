using Application.DTOs.Doctor.DoctorSpecialization;
using Shared.Responses;

namespace Application.Interfaces.Doctor;

/// <summary>
/// Provides operations for doctor specialization assignments.
/// </summary>
public interface IDoctorSpecializationService
{
    Task<ApiResponse<DoctorSpecializationResponseDto>> AddDoctorSpecializationAsync(
        Guid currentUserId,
        Guid doctorUserId,
        AddDoctorSpecializationRequestDto request);
    Task<ApiResponse<List<DoctorSpecializationResponseDto>>> GetDoctorSpecializationsAsync(
        Guid currentUserId,
        Guid doctorUserId);

    
    Task<ApiResponse<DoctorSpecializationResponseDto>> UpdateDoctorSpecializationAsync(
        Guid currentUserId,
        Guid doctorUserId,
        Guid doctorSpecializationId,
        UpdateDoctorSpecializationRequestDto request);

    Task<ApiResponse<bool>> RemoveDoctorSpecializationAsync(
        Guid currentUserId,
        Guid doctorUserId,
        Guid doctorSpecializationId);

    Task<ApiResponse<bool>> DeleteDoctorSpecializationAsync(
        Guid currentUserId,
        Guid doctorUserId, 
        Guid doctorSpecializationId);

}
