using Application.DTOs.ClinicRoom;
using Domain.Entities.Clinic;

namespace Infrastructure.Mappers.Clinic
{
    internal static class ClinicRoomMapper
    {
        internal static ClinicRoomResponseDto ToResponseDto(ClinicRoom clinicRoom)
        {
            ArgumentNullException.ThrowIfNull(clinicRoom);

            return new ClinicRoomResponseDto
            {
                Id = clinicRoom.Id,
                Name = clinicRoom.Name,
                Description = clinicRoom.Description,
                ClinicId = clinicRoom.ClinicId,
            };
        }
    }
}
