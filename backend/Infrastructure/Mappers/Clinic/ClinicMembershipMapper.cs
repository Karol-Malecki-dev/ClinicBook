using Application.DTOs.ClinicMembership;
using Domain.Entities.Clinic;

namespace Infrastructure.Mappers.Clinic
{
    internal static class ClinicMembershipMapper
    {
        internal static ClinicMembershipResponseDto ToResponseDto(ClinicMembership clinicMembership)
        {
            ArgumentNullException.ThrowIfNull(clinicMembership);
            return new ClinicMembershipResponseDto
            {
                Id = clinicMembership.Id,
                UserId = clinicMembership.UserId,
                ClinicId = clinicMembership.ClinicId,
                Role = clinicMembership.Role,
                IsActive = clinicMembership.IsActive,
            };
        }


    }
}
