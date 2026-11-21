namespace Application.DTOs.Specialization;

/// <summary>
/// Contains the data required to create a specialization.
/// </summary>
public sealed class CreateSpecializationRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
