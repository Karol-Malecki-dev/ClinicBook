using Application.DTOs.Other;

namespace Application.DTOs.Patient;

/// <summary>
/// Contains the patient profile fields that can be updated.
/// </summary>
public sealed class UpdatePatientProfileRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public AddressDto? Address { get; init; }
    public DateOnly DateOfBirth { get; init; }
}
