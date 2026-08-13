using Application.DTOs.Other;

namespace Application.DTOs.Clinic.Clinic;

/// <summary>
/// Contains the clinic fields that can be updated.
/// </summary>
public sealed class UpdateClinicRequestDto
{
    public string Name { get; init; } = string.Empty;
    public AddressDto Address { get; init; } = new();
    public string? PhoneNumber { get; init; }
    public string? PhoneNumber2 { get; init; }
    public string? PhoneNumber3 { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public TimeOnly OpenTime { get; init; }
    public TimeOnly CloseTime { get; init; }
}
