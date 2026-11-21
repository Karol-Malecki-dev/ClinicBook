namespace Application.DTOs.Doctor.DoctorProfile;

/// <summary>
/// Contains the data required to create a doctor profile.
/// </summary>
public sealed class CreateDoctorProfileRequestDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public string? PhoneNumber { get; init; }
}
