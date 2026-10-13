namespace Application.Authorization.Clinic;

/// <summary>
/// Represents actions that can be performed within a clinic.
/// </summary>
public enum ClinicPermission
{
    ViewRooms = 1,
    ManageRooms = 2,
    ViewMembers = 3,
    ManageMembers = 4,
    ManageDoctors = 5
}
