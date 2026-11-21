using Domain.Enums;

namespace Application.Authorization.Clinic;

/// <summary>
/// Evaluates permissions based on a clinic membership role.
/// </summary>
public static class ClinicPermissionChecker
{
    /// <summary>
    /// Determines whether a role grants the requested clinic permission.
    /// </summary>
    public static bool HasPermission(ClinicRole role, ClinicPermission permission)
    {
        return permission switch
        {
            ClinicPermission.ViewRooms => role is
                ClinicRole.Patient or
                ClinicRole.Doctor or
                ClinicRole.Receptionist or
                ClinicRole.ClinicAdministrator or
                ClinicRole.Owner,

            ClinicPermission.ManageRooms => role is
                ClinicRole.Doctor or
                ClinicRole.Receptionist or
                ClinicRole.ClinicAdministrator or
                ClinicRole.Owner,

            ClinicPermission.ViewMembers => role is
                ClinicRole.Patient or
                ClinicRole.Doctor or
                ClinicRole.Receptionist or
                ClinicRole.ClinicAdministrator or
                ClinicRole.Owner,

            ClinicPermission.ManageMembers => role is
                ClinicRole.ClinicAdministrator or
                ClinicRole.Owner,

            ClinicPermission.ManageDoctors => role is
                ClinicRole.ClinicAdministrator or
                ClinicRole.Owner,

            _ => false
        };
    }
}
