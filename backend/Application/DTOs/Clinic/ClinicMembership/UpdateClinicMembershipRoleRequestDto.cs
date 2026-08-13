using Domain.Enums;

namespace Application.DTOs.Clinic.ClinicMembership;

/// <summary>
/// Contains the new role for an existing clinic membership.
/// </summary>
public sealed class UpdateClinicMembershipRoleRequestDto
{
    public ClinicRole Role { get; init; }
}
