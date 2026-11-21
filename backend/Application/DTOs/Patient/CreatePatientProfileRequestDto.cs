using Application.DTOs.Other;

namespace Application.DTOs.Patient;

/// <summary>
/// Contains the data required to create a patient profile.
/// </summary>
public sealed class CreatePatientProfileRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public AddressDto? Address { get; init; }
    public DateOnly DateOfBirth { get; init; }
}
