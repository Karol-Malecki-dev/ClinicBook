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
            ArgumentNullException.ThrowIfNull(request);

            var validationError = ValidateUserIds<PatientProfileResponseDto>(currentUserId, patientUserId);
            if (validationError is not null)
            {
                return validationError;
            }

            if (currentUserId != patientUserId)
            {
                return ApiResponse<PatientProfileResponseDto>.Error(
                    403,
                    "A user can create only their own patient profile.");
            }

            var userExists = await _dbContext.Users
                .AnyAsync(user => user.Id == patientUserId && user.IsActive);

            if (!userExists)
            {
                return ApiResponse<PatientProfileResponseDto>.Error(
                    404,
                    "Patient user not found or inactive.");
            }

            var profileExists = await _dbContext.PatientProfiles
                .AnyAsync(profile => profile.UserId == patientUserId);

            if (profileExists)
            {
                return ApiResponse<PatientProfileResponseDto>.Error(
                    409,
                    "A patient profile already exists for this user.");
            }

            var patientProfile = new PatientProfile(
                patientUserId,
                request.Name,
                request.DateOfBirth,
                request.PhoneNumber,
                request.Address is null ? null : AddressMapper.ToAddress(request.Address));

            _dbContext.PatientProfiles.Add(patientProfile);
            await _dbContext.SaveChangesAsync();

            return ApiResponse<PatientProfileResponseDto>.Success(
                PatientProfileMapper.ToResponseDto(patientProfile),
                "Patient profile created successfully.",
                201);

        }

        public async Task<ApiResponse<bool>> DeletePatientProfileAsync(Guid currentUserId, Guid patientUserId)
        {
            var validationError = ValidateUserIds<bool>(currentUserId, patientUserId);
            if (validationError is not null)
            {
                return validationError;
            }

            if (currentUserId != patientUserId)
            {
                return ApiResponse<bool>.Error(
                    403,
                    "A user can delete only their own patient profile.");
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
            var validationError = ValidateUserIds<PatientProfileResponseDto>(currentUserId, patientUserId);
            if (validationError is not null)
            {
                return validationError;
            }

            if (currentUserId != patientUserId)
            {
                return ApiResponse<PatientProfileResponseDto>.Error(
                    403,
                    "A user can update only their own patient profile.");
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
            ArgumentNullException.ThrowIfNull(request);

            var validationError = ValidateUserIds<PatientProfileResponseDto>(currentUserId, patientUserId);
            if (validationError is not null)
            {
                return validationError;
            }

            if (currentUserId != patientUserId)
            {
                return ApiResponse<PatientProfileResponseDto>.Error(
                    403,
                    "A user can update only their own patient profile.");
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

        private static ApiResponse<T>? ValidateUserIds<T>(Guid currentUserId, Guid patientUserId)
        {
            return currentUserId == Guid.Empty || patientUserId == Guid.Empty
                ? ApiResponse<T>.Error(400, "User IDs cannot be empty.")
                : null;
        }
    }
