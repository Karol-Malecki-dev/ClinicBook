namespace Application.DTOs.Doctor.DoctorProfile;

/// <summary>
/// Represents a doctor profile returned by the API.
/// </summary>
public sealed class DoctorProfileResponseDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Bio { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
