using Application.DTOs.Other;

namespace Application.DTOs.ClinicService;

/// <summary>
/// Contains the data required to create a clinic service.
/// </summary>
public sealed class CreateClinicServiceRequestDto
{
    public string Name { get; init; } = string.Empty;
    public AddressDto Address { get; init; } = new();
    public string? Email { get; init; }
}
