namespace Application.DTOs.Clinic;

/// <summary>
/// Public clinic directory information for an active doctor.
/// </summary>
public sealed class ClinicDoctorDto
{
    public Guid DoctorProfileId { get; init; }
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Bio { get; init; } = string.Empty;
    public List<string> Specializations { get; init; } = [];
}
