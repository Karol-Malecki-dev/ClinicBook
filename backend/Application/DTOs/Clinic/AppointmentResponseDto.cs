using Domain.Enums;

namespace Application.DTOs.Clinic;

/// <summary>
/// Represents an appointment with UTC timestamps and clinic-local display times.
/// </summary>
public sealed class AppointmentResponseDto
{
    public Guid Id { get; init; }
    public Guid ClinicId { get; init; }
    public string ClinicName { get; init; } = string.Empty;
    public Guid DoctorProfileId { get; init; }
    public Guid DoctorUserId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public Guid PatientUserId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string PatientEmail { get; init; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; init; }
    public DateTimeOffset EndsAtUtc { get; init; }
    public DateTime LocalStart { get; init; }
    public DateTime LocalEnd { get; init; }
    public string TimeZoneId { get; init; } = string.Empty;
    public AppointmentStatus Status { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}
