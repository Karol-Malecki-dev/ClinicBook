using Application.DTOs.Doctor.DoctorProfile;
using Domain.Entities.Doctor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mappers.Doctor
{
    internal static class DoctorProfileMapper
    {
        internal static DoctorProfileResponseDto ToResponseDto(DoctorProfile doctorProfile)
        {
            ArgumentNullException.ThrowIfNull(doctorProfile, nameof(doctorProfile));
            return new DoctorProfileResponseDto
            {
                Id = doctorProfile.Id,
                UserId = doctorProfile.UserId,
                FirstName = doctorProfile.FirstName,
                LastName = doctorProfile.LastName,
                PhoneNumber = doctorProfile.PhoneNumber,
                IsActive = doctorProfile.IsActive
            };
        }


    }
}
