using Application.DTOs.Clinic.Specialization;
using Shared.Responses;

namespace Application.Interfaces.Specialization;

/// <summary>
/// Provides operations for the specialization catalog.
/// </summary>
public interface ISpecializationService
{
    Task<ApiResponse<List<SpecializationResponseDto>>> GetSpecializationsAsync(
        Guid currentUserId);

    Task<ApiResponse<SpecializationResponseDto>> GetSpecializationByIdAsync(
        Guid currentUserId,
        Guid specializationId);

    Task<ApiResponse<SpecializationResponseDto>> CreateSpecializationAsync(
        Guid currentUserId,
        CreateSpecializationRequestDto request);

    Task<ApiResponse<SpecializationResponseDto>> UpdateSpecializationAsync(
        Guid currentUserId,
        Guid specializationId,
        UpdateSpecializationRequestDto request);

    Task<ApiResponse<bool>> DeleteSpecializationAsync(
        Guid currentUserId,
        Guid specializationId);
}