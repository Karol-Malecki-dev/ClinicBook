using Application.DTOs.Patient;
using Domain.Entities.Patient;

namespace Infrastructure.Mappers.Patient
{
    internal sealed class PatientProfileMapper
    {
        internal static PatientProfileResponseDto ToResponseDto(PatientProfile patientProfile)
        {
            ArgumentNullException.ThrowIfNull(patientProfile, nameof(patientProfile));
            return new PatientProfileResponseDto
            {
                Id = patientProfile.Id,
                UserId = patientProfile.UserId,
                Name = patientProfile.Name,
                PhoneNumber = patientProfile.PhoneNumber,
                Address = patientProfile.Address is null
                    ? null
                    : AddressMapper.ToResponseDto(patientProfile.Address),
                DateOfBirth = patientProfile.DateOfBirth,
                IsActive = patientProfile.IsActive,
            };
        }
    }
}
