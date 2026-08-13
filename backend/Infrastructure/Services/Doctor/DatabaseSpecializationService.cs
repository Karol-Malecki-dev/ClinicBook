using Application.DTOs.Clinic.Specialization;
using Application.Interfaces.Specialization;
using Domain.Entities.Doctor;
using Infrastructure.Data;
using Infrastructure.Mappers.Doctor;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;

namespace Infrastructure.Services.Doctor;

public sealed class DatabaseSpecializationService : ISpecializationService
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseSpecializationService(ApplicationDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<List<SpecializationResponseDto>>> GetSpecializationsAsync(
        Guid currentUserId)
    {
        var currentUserError = ValidateCurrentUserId<List<SpecializationResponseDto>>(currentUserId);
        if (currentUserError is not null)
        {
            return currentUserError;
        }

        var specializations = await _dbContext.Specializations
            .AsNoTracking()
            .OrderBy(specialization => specialization.Name)
            .ToListAsync();

        var response = specializations
            .Select(SpecializationMapper.ToResponseDto)
            .ToList();

        return ApiResponse<List<SpecializationResponseDto>>.Success(
            response,
            "Specializations retrieved successfully.");
    }

    public async Task<ApiResponse<SpecializationResponseDto>> GetSpecializationByIdAsync(
        Guid currentUserId,
        Guid specializationId)
    {
        var currentUserError = ValidateCurrentUserId<SpecializationResponseDto>(currentUserId);
        if (currentUserError is not null)
        {
            return currentUserError;
        }

        if (specializationId == Guid.Empty)
        {
            return ApiResponse<SpecializationResponseDto>.Error(
                400,
                "Specialization ID cannot be empty.");
        }

        var specialization = await _dbContext.Specializations
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == specializationId);

        if (specialization is null)
        {
            return ApiResponse<SpecializationResponseDto>.Error(
                404,
                "Specialization not found.");
        }

        return ApiResponse<SpecializationResponseDto>.Success(
            SpecializationMapper.ToResponseDto(specialization),
            "Specialization retrieved successfully.");
    }

    public async Task<ApiResponse<SpecializationResponseDto>> CreateSpecializationAsync(
        Guid currentUserId,
        CreateSpecializationRequestDto request)
    {
        var currentUserError = ValidateCurrentUserId<SpecializationResponseDto>(currentUserId);
        if (currentUserError is not null)
        {
            return currentUserError;
        }

        if (request is null)
        {
            return ApiResponse<SpecializationResponseDto>.Error(
                400,
                "Request cannot be null.");
        }

        var specialization = new Specialization(request.Name, request.Description);

        _dbContext.Specializations.Add(specialization);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<SpecializationResponseDto>.Success(
            SpecializationMapper.ToResponseDto(specialization),
            "Specialization created successfully.",
            201);
    }

    public async Task<ApiResponse<SpecializationResponseDto>> UpdateSpecializationAsync(
        Guid currentUserId,
        Guid specializationId,
        UpdateSpecializationRequestDto request)
    {
        var currentUserError = ValidateCurrentUserId<SpecializationResponseDto>(currentUserId);
        if (currentUserError is not null)
        {
            return currentUserError;
        }

        if (specializationId == Guid.Empty)
        {
            return ApiResponse<SpecializationResponseDto>.Error(
                400,
                "Specialization ID cannot be empty.");
        }

        if (request is null)
        {
            return ApiResponse<SpecializationResponseDto>.Error(
                400,
                "Request cannot be null.");
        }

        var specialization = await _dbContext.Specializations
            .FirstOrDefaultAsync(item => item.Id == specializationId);

        if (specialization is null)
        {
            return ApiResponse<SpecializationResponseDto>.Error(
                404,
                "Specialization not found.");
        }

        specialization.Rename(request.Name);
        specialization.UpdateDescription(request.Description);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<SpecializationResponseDto>.Success(
            SpecializationMapper.ToResponseDto(specialization),
            "Specialization updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteSpecializationAsync(
        Guid currentUserId,
        Guid specializationId)
    {
        var currentUserError = ValidateCurrentUserId<bool>(currentUserId);
        if (currentUserError is not null)
        {
            return currentUserError;
        }

        if (specializationId == Guid.Empty)
        {
            return ApiResponse<bool>.Error(
                400,
                "Specialization ID cannot be empty.");
        }

        var specialization = await _dbContext.Specializations
            .FirstOrDefaultAsync(item => item.Id == specializationId);

        if (specialization is null)
        {
            return ApiResponse<bool>.Error(
                404,
                "Specialization not found.");
        }

        _dbContext.Specializations.Remove(specialization);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<bool>.Success(
            true,
            "Specialization deleted successfully.");
    }

    private static ApiResponse<T>? ValidateCurrentUserId<T>(Guid currentUserId)
    {
        return currentUserId == Guid.Empty
            ? ApiResponse<T>.Error(400, "Current user ID cannot be empty.")
            : null;
    }
}