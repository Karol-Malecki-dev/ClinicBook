namespace Application.DTOs.Doctor.DoctorSpecialization;

/// <summary>
/// Contains the doctor-specific specialization data that can be updated.
/// </summary>
public sealed class UpdateDoctorSpecializationRequestDto
{
    public string? ExperienceDescription { get; init; }
}
