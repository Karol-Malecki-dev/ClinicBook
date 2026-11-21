using Application.DTOs.Other;

namespace Application.DTOs.Patient;

/// <summary>
/// Represents a patient profile returned by the API.
/// </summary>
public sealed class PatientProfileResponseDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public AddressDto? Address { get; init; }
    public DateOnly DateOfBirth { get; init; }
    public bool IsActive { get; init; }
}
