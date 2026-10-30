using Application.DTOs.Doctor.DoctorProfile;
using Application.Interfaces.Doctor;
using Domain.Entities.Doctor;
using Infrastructure.Data;
using Infrastructure.Mappers.Doctor;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;

namespace Infrastructure.Services.Doctor;

public sealed class DatabaseDoctorProfileService : IDoctorProfileService
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseDoctorProfileService(ApplicationDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<DoctorProfileResponseDto>> GetDoctorProfileAsync(
        Guid currentUserId,
        Guid doctorUserId)
    {
        var validationError = ValidateUserIds<DoctorProfileResponseDto>(currentUserId, doctorUserId);
        if (validationError is not null)
        {
            return validationError;
        }

        if (currentUserId != doctorUserId)
        {
            return ApiResponse<DoctorProfileResponseDto>.Error(
                403,
                "A user can view only their own doctor profile.");
        }

        var profile = await _dbContext.DoctorProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == doctorUserId && item.IsActive);

        return profile is null
            ? ApiResponse<DoctorProfileResponseDto>.Error(404, "Doctor profile not found.")
            : ApiResponse<DoctorProfileResponseDto>.Success(
                DoctorProfileMapper.ToResponseDto(profile),
                "Doctor profile retrieved successfully.");
    }

    public async Task<ApiResponse<DoctorProfileResponseDto>> CreateDoctorProfileAsync(
        Guid currentUserId,
        Guid doctorUserId,
        CreateDoctorProfileRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationError = ValidateUserIds<DoctorProfileResponseDto>(currentUserId, doctorUserId);
        if (validationError is not null)
        {
            return validationError;
        }

        if (currentUserId != doctorUserId)
        {
            return ApiResponse<DoctorProfileResponseDto>.Error(
                403,
                "A user can create only their own doctor profile.");
        }

        var userExists = await _dbContext.Users
            .AnyAsync(user => user.Id == doctorUserId && user.IsActive);

        if (!userExists)
        {
            return ApiResponse<DoctorProfileResponseDto>.Error(
                404,
                "Doctor user not found or inactive.");
        }

        var profileExists = await _dbContext.DoctorProfiles
            .AnyAsync(item => item.UserId == doctorUserId);

        if (profileExists)
        {
            return ApiResponse<DoctorProfileResponseDto>.Error(
                409,
                "A doctor profile already exists for this user.");
        }

        var profile = new DoctorProfile(
            doctorUserId,
            request.FirstName,
            request.LastName,
            request.Bio,
            request.PhoneNumber);

        _dbContext.DoctorProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<DoctorProfileResponseDto>.Success(
            DoctorProfileMapper.ToResponseDto(profile),
            "Doctor profile created successfully.",
            201);
    }

    public async Task<ApiResponse<DoctorProfileResponseDto>> UpdateDoctorProfileAsync(
        Guid currentUserId,
        Guid doctorUserId,
        UpdateDoctorProfileRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationError = ValidateUserIds<DoctorProfileResponseDto>(currentUserId, doctorUserId);
        if (validationError is not null)
        {
            return validationError;
        }

        if (currentUserId != doctorUserId)
        {
            return ApiResponse<DoctorProfileResponseDto>.Error(
                403,
                "A user can update only their own doctor profile.");
        }

        var profile = await _dbContext.DoctorProfiles
            .FirstOrDefaultAsync(item => item.UserId == doctorUserId && item.IsActive);

        if (profile is null)
        {
            return ApiResponse<DoctorProfileResponseDto>.Error(
                404,
                "Doctor profile not found.");
        }

        profile.UpdateDetails(
            request.FirstName,
            request.LastName,
            request.Bio,
            request.PhoneNumber);

        await _dbContext.SaveChangesAsync();

        return ApiResponse<DoctorProfileResponseDto>.Success(
            DoctorProfileMapper.ToResponseDto(profile),
            "Doctor profile updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteDoctorProfileAsync(
        Guid currentUserId,
        Guid doctorUserId)
    {
        var validationError = ValidateUserIds<bool>(currentUserId, doctorUserId);
        if (validationError is not null)
        {
            return validationError;
        }

        if (currentUserId != doctorUserId)
        {
            return ApiResponse<bool>.Error(
                403,
                "A user can delete only their own doctor profile.");
        }

        var profile = await _dbContext.DoctorProfiles
            .FirstOrDefaultAsync(item => item.UserId == doctorUserId && item.IsActive);

        if (profile is null)
        {
            return ApiResponse<bool>.Error(404, "Doctor profile not found.");
        }

        profile.Deactivate();
        await _dbContext.SaveChangesAsync();

        return ApiResponse<bool>.Success(true, "Doctor profile deleted successfully.");
    }

    private static ApiResponse<T>? ValidateUserIds<T>(Guid currentUserId, Guid doctorUserId)
    {
        return currentUserId == Guid.Empty || doctorUserId == Guid.Empty
            ? ApiResponse<T>.Error(400, "User IDs cannot be empty.")
            : null;
    }
}