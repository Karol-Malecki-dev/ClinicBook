using Application.DTOs.Clinic;
using Application.DTOs.Other;
using ClinicEntity = Domain.Entities.Clinic.Clinic;

namespace Infrastructure.Mappers.Clinic
{
    internal static class ClinicMapper
    {
        internal static ClinicResponseDto ToResponseDto(ClinicEntity clinic)
        {
            ArgumentNullException.ThrowIfNull(clinic, nameof(clinic));

            return new ClinicResponseDto
            {
                Id = clinic.Id,
                Name = clinic.Name,
                Email = clinic.Email,
                Address = AddressMapper.ToResponseDto(clinic.Address),
                PhoneNumber = clinic.PhoneNumber,
                PhoneNumber2 = clinic.PhoneNumber2,
                PhoneNumber3 = clinic.PhoneNumber3,
                Website = clinic.Website,
                OpenTime = clinic.OpenTime,
                CloseTime = clinic.CloseTime,

            };
        }
    }
}
