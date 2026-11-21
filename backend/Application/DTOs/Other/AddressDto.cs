namespace Application.DTOs.Other;

/// <summary>
/// Represents an address transferred through the application API.
/// </summary>
public sealed class AddressDto
{
    public string Street { get; init; } = string.Empty;

    public string BuildingNumber { get; init; } = string.Empty;

    public string? ApartmentNumber { get; init; }

    public string City { get; init; } = string.Empty;

    public string PostalCode { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;
}
