using Application.DTOs.Doctor.DoctorSpecialization;
using Domain.Entities.Doctor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mappers.Doctor
{
    internal static class DoctorSpecializationMapper
    {
        internal static DoctorSpecializationResponseDto ToResponseDto(DoctorSpecialization doctorSpecialization)
        {
            ArgumentNullException.ThrowIfNull(doctorSpecialization, nameof(doctorSpecialization));
            return new DoctorSpecializationResponseDto
            {
                Id = doctorSpecialization.Id,
                DoctorProfileId = doctorSpecialization.DoctorProfileId,
                SpecializationId = doctorSpecialization.SpecializationId,
                SpecializationName = doctorSpecialization.Specialization.Name,
                ExperienceDescription = doctorSpecialization.ExperienceDescription
            };
        }

        internal static List<DoctorSpecializationResponseDto> ToResponseListDto(List<DoctorSpecialization> doctorSpecializations)
        {
            ArgumentNullException.ThrowIfNull(doctorSpecializations, nameof(doctorSpecializations));
            return doctorSpecializations.Select(ds => ToResponseDto(ds)).ToList();
        }
    }
}
