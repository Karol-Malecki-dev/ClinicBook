using Application.DTOs.Patient;
using Application.Interfaces.Patient;
using Domain.Entities.Patient;
using Infrastructure.Data;
using Infrastructure.Mappers;
using Infrastructure.Mappers.Patient;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;

namespace Infrastructure.Services.Patient;

public sealed class DatabasePatientProfileService : IPatientProfileService
{
    private readonly ApplicationDbContext _dbContext;

    public DatabasePatientProfileService(ApplicationDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }
        public async Task<ApiResponse<PatientProfileResponseDto>> CreatePatientProfileAsync(Guid currentUserId, Guid patientUserId, CreatePatientProfileRequestDto request)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            if(currentUserId == Guid.Empty)
            {
                return ApiResponse<PatientProfileResponseDto>.Error(400, "Current user ID cannot be empty.");
            }
            
            if(patientUserId == Guid.Empty) {
                return ApiResponse<PatientProfileResponseDto>.Error(400, "Patient user ID cannot be empty.");
            }

            var patientProfile = new PatientProfile(
                patientUserId,
                request.Name,
                request.DateOfBirth,
                request.PhoneNumber,
                request.Address is null ? null : AddressMapper.ToAddress(request.Address));

            _dbContext.PatientProfiles.Add(patientProfile);
            await _dbContext.SaveChangesAsync();

            return ApiResponse<PatientProfileResponseDto>.Success(PatientProfileMapper.ToResponseDto(patientProfile), "Patient profile created successfully.");

        }

        public async Task<ApiResponse<bool>> DeletePatientProfileAsync(Guid currentUserId, Guid patientUserId)
        {
            if (currentUserId == Guid.Empty)
            {
                return ApiResponse<bool>.Error(400, "Current user ID cannot be empty.");
            }

            if (patientUserId == Guid.Empty)
            {
                return ApiResponse<bool>.Error(400, "Patient user ID cannot be empty.");
            }

            var patientProfile = await _dbContext.PatientProfiles
                .FirstOrDefaultAsync(profile => profile.UserId == patientUserId);

            if(patientProfile == null)
            {
                return ApiResponse<bool>.Error(404, "Patient profile not found.");
            }

            _dbContext.PatientProfiles.Remove(patientProfile);
            await _dbContext.SaveChangesAsync();

            return ApiResponse<bool>.Success(true, "Patient profile deleted successfully.");




        }

        public async Task<ApiResponse<PatientProfileResponseDto>> GetPatientProfileAsync(Guid currentUserId, Guid patientUserId)
        {
            if (currentUserId == Guid.Empty)
            {
                return ApiResponse<PatientProfileResponseDto>.Error(400, "Current user ID cannot be empty.");
            }

            if (patientUserId == Guid.Empty)
            {
                return ApiResponse<PatientProfileResponseDto>.Error(400, "Patient user ID cannot be empty.");
            }

            var patientProfile = await _dbContext.PatientProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(profile => profile.UserId == patientUserId);

            if(patientProfile == null)
            {
                return ApiResponse<PatientProfileResponseDto>.Error( 404, "Patient profile not found.");
            }

            return ApiResponse<PatientProfileResponseDto>.Success(PatientProfileMapper.ToResponseDto(patientProfile), "Patient profile retrieved successfully.");
        }

        public async Task<ApiResponse<PatientProfileResponseDto>> UpdatePatientProfileAsync(Guid currentUserId, Guid patientUserId, UpdatePatientProfileRequestDto request)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            if (currentUserId == Guid.Empty)
            {
                return ApiResponse<PatientProfileResponseDto>.Error(400, "Current user ID cannot be empty.");
            }

            if (patientUserId == Guid.Empty)
            {
                return ApiResponse<PatientProfileResponseDto>.Error(400, "Patient user ID cannot be empty.");
            }

            var patientProfile = await _dbContext.PatientProfiles
                .FirstOrDefaultAsync(profile => profile.UserId == patientUserId);

            if (patientProfile == null)
            {
                return ApiResponse<PatientProfileResponseDto>.Error(
                    404,
                    "Patient profile not found.");
            }

            patientProfile.UpdateDetails(
                request.Name,
                request.DateOfBirth,
                request.PhoneNumber,
                request.Address is null ? null : AddressMapper.ToAddress(request.Address));

            await _dbContext.SaveChangesAsync();

            return ApiResponse<PatientProfileResponseDto>.Success(PatientProfileMapper.ToResponseDto(patientProfile), "Patient profile updated successfully.");
        }
    }
