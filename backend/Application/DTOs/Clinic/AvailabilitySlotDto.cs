namespace Application.DTOs.Clinic;

/// <summary>
/// Represents an unbooked slot, including its UTC instant and clinic-local display time.
/// </summary>
public sealed class AvailabilitySlotDto
{
    public DateTimeOffset StartsAtUtc { get; init; }
    public DateTimeOffset EndsAtUtc { get; init; }
    public DateTime LocalStart { get; init; }
    public DateTime LocalEnd { get; init; }
    public string TimeZoneId { get; init; } = string.Empty;
}
