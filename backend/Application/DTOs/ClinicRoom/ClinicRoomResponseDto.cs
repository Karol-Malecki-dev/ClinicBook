namespace Application.DTOs.ClinicRoom;

/// <summary>
/// Represents a clinic room returned by the API.
/// </summary>
public sealed class ClinicRoomResponseDto
{
    public Guid Id { get; init; }
    public Guid ClinicId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
