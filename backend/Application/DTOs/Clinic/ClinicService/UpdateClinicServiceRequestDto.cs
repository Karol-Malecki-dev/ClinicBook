using Application.DTOs.Other;

namespace Application.DTOs.Clinic.ClinicService;

/// <summary>
/// Contains the clinic service fields that can be updated.
/// </summary>
public sealed class UpdateClinicServiceRequestDto
{
    public string Name { get; init; } = string.Empty;
    public AddressDto Address { get; init; } = new();
    public string? Email { get; init; }
}
