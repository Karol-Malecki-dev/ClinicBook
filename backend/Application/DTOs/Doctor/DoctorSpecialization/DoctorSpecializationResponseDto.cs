namespace Application.DTOs.Doctor.DoctorSpecialization;

/// <summary>
/// Represents a doctor's specialization assignment returned by the API.
/// </summary>
public sealed class DoctorSpecializationResponseDto
{
    public Guid Id { get; init; }
    public Guid DoctorProfileId { get; init; }
    public Guid SpecializationId { get; init; }
    public string? SpecializationName { get; init; } = string.Empty;
    public string? ExperienceDescription { get; init; } = string.Empty;
}
