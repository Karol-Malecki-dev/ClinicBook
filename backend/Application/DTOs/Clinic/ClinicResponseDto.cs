using Application.DTOs.Other;

namespace Application.DTOs.Clinic;

/// <summary>
/// Represents clinic data returned by the API.
/// </summary>
public sealed class ClinicResponseDto
{
    public Guid Id { get; init; }
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
