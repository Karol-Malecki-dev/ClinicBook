using Domain.Enums;

namespace Application.DTOs.Clinic.ClinicMembership;

/// <summary>
/// Contains the data required to add a user to a clinic.
/// </summary>
public sealed class AddClinicMembershipRequestDto
{
    public Guid UserId { get; init; }
    public ClinicRole Role { get; init; }
}
