namespace Application.DTOs.Clinic.ClinicRoom;

/// <summary>
/// Contains the clinic room fields that can be updated.
/// </summary>
public sealed class UpdateClinicRoomRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
