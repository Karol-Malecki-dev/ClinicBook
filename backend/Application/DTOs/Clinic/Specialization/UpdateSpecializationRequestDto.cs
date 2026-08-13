namespace Application.DTOs.Clinic.Specialization;

/// <summary>
/// Contains the specialization fields that can be updated.
/// </summary>
public sealed class UpdateSpecializationRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
