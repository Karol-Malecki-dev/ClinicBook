using Application.DTOs.ClinicRoom;
using Application.Interfaces.Clinic;
using Domain.Entities;
using Domain.Entities.Clinic;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;
using Shared.Responses;

namespace Infrastructure.Services.Clinic
{
    public sealed class DatabaseClinicRoomService : IClinicRoomService
    {
        private readonly ApplicationDbContext _dbContext;

        public DatabaseClinicRoomService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ApiResponse<ClinicRoomResponseDto>> CreateClinicRoomAsync(Guid currentUserId, Guid clinicId, CreateClinicRoomRequestDto request)
        {
            var clinicMembership = await _dbContext.ClinicMemberships
                .FirstOrDefaultAsync(cm => cm.UserId == currentUserId && cm.ClinicId == clinicId);
            
            if (clinicMembership is null)
            {
                return ApiResponse<ClinicRoomResponseDto>.Error(403, "User is not a member of the clinic.");
            }

            if(clinicMembership.Role == ClinicRole.Patient)
            {
                return ApiResponse<ClinicRoomResponseDto>.Error(403, "User does not have permission to create a room in this clinic.");
            }

            var clinic = await _dbContext.Clinics.FindAsync(clinicId);

            if(clinic is null)
            {
                return ApiResponse<ClinicRoomResponseDto>.Error(404, "Clinic not found.");
            }

            if(request is null || request.Name.Length > 100)
            {
                return ApiResponse<ClinicRoomResponseDto>.Error(400, "Room name must be max 100 characters.");
            }
            
            var newRoom = new ClinicRoom(clinicId, request.Name, request.Description);

            await _dbContext.ClinicRooms.AddAsync(newRoom);
            await _dbContext.SaveChangesAsync();

            return ApiResponse<ClinicRoomResponseDto>.Success(new ClinicRoomResponseDto
            {
                Id = newRoom.Id,
                Name = newRoom.Name,
                Description = newRoom.Description,
                ClinicId = newRoom.ClinicId
            }, "Clinic room created successfully.", 201);
        }

        public async Task<ApiResponse<bool>> DeleteClinicRoomAsync(Guid currentUserId, Guid clinicId, Guid roomId)
        {
            var clinicMembership = await _dbContext.ClinicMemberships
                .FirstOrDefaultAsync(cm => cm.UserId == currentUserId && cm.ClinicId == clinicId);

            if (clinicMembership is null)
            {
                return ApiResponse<bool>.Error(403, "User is not a member of the clinic.");
            }

            if (clinicMembership.Role == ClinicRole.Patient)
            {
                return ApiResponse<bool>.Error(403, "User does not have permission to create a room in this clinic.");
            }

            var clinicRoom = await _dbContext.ClinicRooms
                .FirstOrDefaultAsync(room =>
                room.Id == roomId &&
                room.ClinicId == clinicId);

            if (clinicRoom is null)
            {
                return ApiResponse<bool>.Error(404, "Clinic room not found.");
            }

            _dbContext.ClinicRooms.Remove(clinicRoom);
            await _dbContext.SaveChangesAsync();

            return ApiResponse<bool>.Success(true, "Clinic room deleted successfully.");
        }

        public async Task<ApiResponse<ClinicRoomResponseDto>> GetClinicRoomByIdAsync(Guid currentUserId, Guid clinicId, Guid roomId)
        {
            var clinicMembership = await _dbContext.ClinicMemberships
                .FirstOrDefaultAsync(cm => cm.UserId == currentUserId && cm.ClinicId == clinicId);

            if (clinicMembership is null)
            {
                return ApiResponse<ClinicRoomResponseDto>.Error(403, "User is not a member of the clinic.");
            }

            var clinicRoom = await _dbContext.ClinicRooms
                .FirstOrDefaultAsync(room =>
                room.Id == roomId &&
                room.ClinicId == clinicId);

            if (clinicRoom is null)
            {
                return ApiResponse<ClinicRoomResponseDto>.Error(404, "Clinic room not found.");
            }

            return ApiResponse<ClinicRoomResponseDto>.Success(new ClinicRoomResponseDto
            {
                Id = clinicRoom.Id,
                Name = clinicRoom.Name,
                Description = clinicRoom.Description,
                ClinicId = clinicRoom.ClinicId
            }, "Clinic room retrieved successfully.");
        }

        public async Task<ApiResponse<List<ClinicRoomResponseDto>>> GetClinicRoomsAsync(Guid currentUserId, Guid clinicId)
        {
            var clinicMembership = await _dbContext.ClinicMemberships
                .FirstOrDefaultAsync(cm => cm.UserId == currentUserId && cm.ClinicId == clinicId);

            if (clinicMembership is null)
            {
                return ApiResponse<List<ClinicRoomResponseDto>>.Error(403, "User is not a member of the clinic.");
            }
            var clinicRooms = await _dbContext.ClinicRooms
                .Where(cr => cr.ClinicId == clinicId && cr.ClinicId == clinicId)
                .ToListAsync();
            var response = clinicRooms.Select(cr => new ClinicRoomResponseDto
            {
                Id = cr.Id,
                Name = cr.Name,
                Description = cr.Description,
                ClinicId = cr.ClinicId
            }).ToList();
            return ApiResponse<List<ClinicRoomResponseDto>>.Success(response, "Clinic rooms retrieved successfully.");
        }

        public async Task<ApiResponse<ClinicRoomResponseDto>> UpdateClinicRoomAsync(Guid currentUserId, Guid clinicId, Guid roomId, UpdateClinicRoomRequestDto request)
        {
            var clinicMembership = await _dbContext.ClinicMemberships
                .FirstOrDefaultAsync(cm => cm.UserId == currentUserId && cm.ClinicId == clinicId);

            if (clinicMembership is null)
            {
                return ApiResponse<ClinicRoomResponseDto>.Error(403, "User is not a member of the clinic.");
            }

            if (clinicMembership.Role == ClinicRole.Patient)
            {
                return ApiResponse<ClinicRoomResponseDto>.Error(403, "User does not have permission to create a room in this clinic.");
            }

            var clinic = await _dbContext.Clinics.FindAsync(clinicId);

            if (clinic is null)
            {
                return ApiResponse<ClinicRoomResponseDto>.Error(404, "Clinic not found.");
            }

            var clinicRoom = await _dbContext.ClinicRooms
                 .FirstOrDefaultAsync(room =>
                 room.Id == roomId &&
                 room.ClinicId == clinicId);

            if (clinicRoom is null)
            {
                return ApiResponse<ClinicRoomResponseDto>.Error(404, "Clinic room not found.");
            }

            clinicRoom.Rename(request.Name);
            clinicRoom.UpdateDescription(request.Description);

            _dbContext.ClinicRooms.Update(clinicRoom);
            await _dbContext.SaveChangesAsync();

            return ApiResponse<ClinicRoomResponseDto>.Success(new ClinicRoomResponseDto
            {
                Id = clinicRoom.Id,
                Name = clinicRoom.Name,
                Description = clinicRoom.Description,
                ClinicId = clinicRoom.ClinicId
            }, "Clinic room updated successfully.");
        }
    }
}
