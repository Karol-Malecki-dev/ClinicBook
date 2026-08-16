using Application.DTOs.Specialization;
using Domain.Entities.Doctor;

namespace Infrastructure.Mappers.Doctor
{
    internal static class SpecializationMapper
    {
        internal static SpecializationResponseDto ToResponseDto(this Specialization specialization)
        {
            if (specialization == null)
            {
                throw new ArgumentNullException(nameof(specialization));
            }
            return new SpecializationResponseDto
            {
                Id = specialization.Id,
                Name = specialization.Name,
                Description = specialization.Description
            };
        }
    }
}
