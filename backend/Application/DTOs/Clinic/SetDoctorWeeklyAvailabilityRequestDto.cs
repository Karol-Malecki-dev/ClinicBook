namespace Application.DTOs.Clinic;

/// <summary>
/// Defines a recurring weekday window and the duration of each bookable slot.
/// Times are interpreted in the clinic's configured time zone.
/// </summary>
public sealed class SetDoctorWeeklyAvailabilityRequestDto
{
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public int SlotDurationMinutes { get; init; }
}
