using Domain.Enums;

namespace Application.DTOs.Clinic;

/// <summary>
/// Represents a one-off doctor availability change in clinic-local time.
/// </summary>
public sealed class DoctorScheduleExceptionDto
{
    public Guid Id { get; init; }
    public Guid ClinicId { get; init; }
    public Guid DoctorProfileId { get; init; }
    public Guid DoctorUserId { get; init; }
    public DateOnly Date { get; init; }
    public ScheduleExceptionType Type { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public int SlotDurationMinutes { get; init; }
    public string? Reason { get; init; }
}
