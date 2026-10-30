namespace Application.DTOs.Clinic;

/// <summary>
/// Requests a booking for an exact UTC start instant returned by the availability API.
/// </summary>
public sealed class CreateAppointmentRequestDto
{
    public DateTimeOffset StartsAtUtc { get; init; }
}
