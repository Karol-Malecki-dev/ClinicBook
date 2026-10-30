using Application.DTOs.Clinic;
using Shared.Responses;

namespace Application.Interfaces.Clinic;

/// <summary>
/// Manages doctor schedules and returns bookable clinic-local appointment slots.
/// </summary>
public interface IDoctorAvailabilityService
{
    Task<ApiResponse<List<ClinicDoctorDto>>> GetClinicDoctorsAsync(
        Guid currentUserId,
        Guid clinicId);

    Task<ApiResponse<List<DoctorWeeklyAvailabilityDto>>> GetWeeklyAvailabilityAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid doctorUserId);

    Task<ApiResponse<DoctorWeeklyAvailabilityDto>> CreateWeeklyAvailabilityAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid doctorUserId,
        SetDoctorWeeklyAvailabilityRequestDto request);

    Task<ApiResponse<DoctorWeeklyAvailabilityDto>> UpdateWeeklyAvailabilityAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid doctorUserId,
        Guid availabilityId,
        SetDoctorWeeklyAvailabilityRequestDto request);

    Task<ApiResponse<bool>> DeleteWeeklyAvailabilityAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid doctorUserId,
        Guid availabilityId);

    Task<ApiResponse<List<AvailabilitySlotDto>>> GetAvailableSlotsAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid doctorUserId,
        DateOnly date);
}
