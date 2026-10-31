namespace Application.DTOs.Clinic;

/// <summary>
/// Represents one recurring doctor availability window in the clinic's local time.
/// </summary>
public sealed class DoctorWeeklyAvailabilityDto
{
    public Guid Id { get; init; }
    public Guid ClinicId { get; init; }
    public Guid DoctorProfileId { get; init; }
    public Guid DoctorUserId { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public int SlotDurationMinutes { get; init; }
}
