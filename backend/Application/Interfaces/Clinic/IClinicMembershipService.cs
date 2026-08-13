using Application.DTOs.Clinic.ClinicMembership;
using Shared.Responses;

namespace Application.Interfaces.Clinic;

/// <summary>
/// Provides operations for memberships within a specific clinic.
/// </summary>
public interface IClinicMembershipService
{
    Task<ApiResponse<List<ClinicMembershipResponseDto>>> GetClinicMembershipsAsync(
        Guid currentUserId,
        Guid clinicId);

    Task<ApiResponse<ClinicMembershipResponseDto>> GetClinicMembershipByUserIdAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid memberUserId);

    Task<ApiResponse<ClinicMembershipResponseDto>> CreateClinicMembershipAsync(
        Guid currentUserId,
        Guid clinicId,
        AddClinicMembershipRequestDto request);

    Task<ApiResponse<ClinicMembershipResponseDto>> UpdateClinicMembershipAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid memberUserId,
        UpdateClinicMembershipRoleRequestDto request);

    Task<ApiResponse<bool>> DeleteClinicMembershipAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid memberUserId);

}
