using Domain.Enums;

namespace Application.DTOs.Clinic;

/// <summary>
/// Requests a one-off availability addition or closure for a doctor.
/// </summary>
public sealed class CreateDoctorScheduleExceptionRequestDto
{
    public DateOnly Date { get; init; }
    public ScheduleExceptionType Type { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public int SlotDurationMinutes { get; init; }
    public string? Reason { get; init; }
}
