using Application.DTOs.Clinic;
using Shared.Responses;

namespace Application.Interfaces.Clinic;

/// <summary>
/// Provides operations for clinics while enforcing access in the application layer.
/// </summary>
public interface IClinicService
{
    Task<ApiResponse<List<ClinicResponseDto>>> GetDiscoverableClinicsAsync(
        Guid currentUserId);

    Task<ApiResponse<List<ClinicResponseDto>>> GetAccessibleClinicsAsync(
        Guid currentUserId);

    Task<ApiResponse<ClinicResponseDto>> GetClinicByIdAsync(
        Guid currentUserId,
        Guid clinicId);

    Task<ApiResponse<ClinicResponseDto>> CreateClinicAsync(
        Guid currentUserId,
        CreateClinicRequestDto request);

    Task<ApiResponse<ClinicResponseDto>> UpdateClinicAsync(
        Guid currentUserId,
        Guid clinicId,
        UpdateClinicRequestDto request);
}
