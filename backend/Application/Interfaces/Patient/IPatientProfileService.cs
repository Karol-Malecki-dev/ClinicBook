using Application.DTOs.Patient;
using Shared.Responses;

namespace Application.Interfaces.Patient;

/// <summary>
/// Provides operations for patient profiles.
/// </summary>
public interface IPatientProfileService
{
    Task<ApiResponse<PatientProfileResponseDto>> GetPatientProfileAsync(
        Guid currentUserId,
        Guid patientUserId);

    Task<ApiResponse<PatientProfileResponseDto>> CreatePatientProfileAsync(
        Guid currentUserId,
        Guid patientUserId,
        CreatePatientProfileRequestDto request);

    Task<ApiResponse<PatientProfileResponseDto>> UpdatePatientProfileAsync(
        Guid currentUserId,
        Guid patientUserId,
        UpdatePatientProfileRequestDto request);

    Task<ApiResponse<bool>> DeletePatientProfileAsync(
        Guid currentUserId,
        Guid patientUserId);
}
