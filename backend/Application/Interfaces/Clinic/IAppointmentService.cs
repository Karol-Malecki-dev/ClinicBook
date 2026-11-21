using Application.DTOs.Clinic;
using Shared.Responses;

namespace Application.Interfaces.Clinic;

/// <summary>
/// Provides patient and clinic staff appointment workflows.
/// </summary>
public interface IAppointmentService
{
    Task<ApiResponse<AppointmentResponseDto>> BookAppointmentAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid doctorUserId,
        CreateAppointmentRequestDto request);

    Task<ApiResponse<List<AppointmentResponseDto>>> GetPatientAppointmentsAsync(
        Guid currentUserId);

    Task<ApiResponse<List<AppointmentResponseDto>>> GetClinicAppointmentsAsync(
        Guid currentUserId,
        Guid clinicId,
        DateOnly date);

    Task<ApiResponse<AppointmentResponseDto>> CancelAppointmentAsync(
        Guid currentUserId,
        Guid appointmentId);

    Task<ApiResponse<AppointmentResponseDto>> CompleteAppointmentAsync(
        Guid currentUserId,
        Guid appointmentId);
}
