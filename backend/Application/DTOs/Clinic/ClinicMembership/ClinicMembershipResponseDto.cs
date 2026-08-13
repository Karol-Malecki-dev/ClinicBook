using Domain.Enums;

namespace Application.DTOs.Clinic.ClinicMembership;

/// <summary>
/// Represents a clinic membership returned by the API.
/// </summary>
public sealed class ClinicMembershipResponseDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid ClinicId { get; init; }
    public ClinicRole Role { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
