namespace Application.DTOs.Doctor.DoctorProfile;

/// <summary>
/// Contains the doctor profile fields that can be updated.
/// </summary>
public sealed class UpdateDoctorProfileRequestDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public string? PhoneNumber { get; init; }
}
