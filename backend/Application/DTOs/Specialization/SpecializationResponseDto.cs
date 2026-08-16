namespace Application.DTOs.Specialization;

/// <summary>
/// Represents a specialization returned by the API.
/// </summary>
public sealed class SpecializationResponseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
