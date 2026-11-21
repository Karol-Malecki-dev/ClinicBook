using Application.Authorization.Clinic;
using Domain.Entities.Clinic;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Authorization;

public static class ClinicAccess
{
    public static Task<ClinicMembership?> GetMembershipAsync(
        ApplicationDbContext dbContext,
        Guid userId,
        Guid clinicId)
        => dbContext.ClinicMemberships
            .FirstOrDefaultAsync(membership =>
                membership.UserId == userId
                && membership.ClinicId == clinicId
                && membership.IsActive
                && membership.User.IsActive);

    public static bool CanManage(ClinicRole role)
        => ClinicPermissionChecker.HasPermission(role, ClinicPermission.ManageMembers);

    public static bool CanManageMembers(ClinicRole role)
        => ClinicPermissionChecker.HasPermission(role, ClinicPermission.ManageMembers);

    public static bool CanManageRooms(ClinicRole role)
        => ClinicPermissionChecker.HasPermission(role, ClinicPermission.ManageRooms);

    public static bool CanManageDoctors(ClinicRole role)
        => ClinicPermissionChecker.HasPermission(role, ClinicPermission.ManageDoctors);
}
