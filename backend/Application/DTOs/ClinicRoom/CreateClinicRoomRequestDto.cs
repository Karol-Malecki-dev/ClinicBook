namespace Application.DTOs.ClinicRoom;

/// <summary>
/// Contains the data required to create a clinic room.
/// </summary>
public sealed class CreateClinicRoomRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
