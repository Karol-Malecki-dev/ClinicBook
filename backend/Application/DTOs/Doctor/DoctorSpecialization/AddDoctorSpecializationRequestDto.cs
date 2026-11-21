namespace Application.DTOs.Doctor.DoctorSpecialization;

/// <summary>
/// Contains the data required to assign a specialization to a doctor.
/// </summary>
public sealed class AddDoctorSpecializationRequestDto
{
    public Guid SpecializationId { get; init; }
    public string? ExperienceDescription { get; init; }
}
