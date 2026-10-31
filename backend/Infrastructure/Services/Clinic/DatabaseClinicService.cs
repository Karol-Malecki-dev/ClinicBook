using Application.DTOs.Clinic;
using Application.DTOs.Other;
using Application.Interfaces.Clinic;
using Domain.Entities.Clinic;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Authorization;
using Infrastructure.Data;
using Infrastructure.Mappers;
using Infrastructure.Mappers.Clinic;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;

namespace Infrastructure.Services.Clinic;

public sealed class DatabaseClinicService : IClinicService
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseClinicService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<List<ClinicResponseDto>>> GetDiscoverableClinicsAsync(
        Guid currentUserId)
    {
        var userIsActive = await _dbContext.Users
            .AnyAsync(user => user.Id == currentUserId && user.IsActive);
        if (!userIsActive)
        {
            return ApiResponse<List<ClinicResponseDto>>.Error(401, "User not authenticated.");
        }

        var clinics = await _dbContext.Clinics
            .AsNoTracking()
            .OrderBy(clinic => clinic.Name)
            .ToListAsync();

        return ApiResponse<List<ClinicResponseDto>>.Success(
            clinics.Select(clinic => ClinicMapper.ToResponseDto(clinic)).ToList());
    }

    public async Task<ApiResponse<List<ClinicResponseDto>>> GetAccessibleClinicsAsync(
        Guid currentUserId)
    {
        var clinics = await _dbContext.Clinics
            .AsNoTracking()
            .Where(clinic => clinic.Memberships.Any(membership =>
                membership.UserId == currentUserId
                && membership.IsActive
                && membership.User.IsActive))
            .Include(clinic => clinic.Memberships.Where(membership =>
                membership.UserId == currentUserId
                && membership.IsActive
                && membership.User.IsActive))
            .OrderBy(clinic => clinic.Name)
            .ToListAsync();

        var response = clinics
            .Select(clinic => ClinicMapper.ToResponseDto(
                clinic,
                clinic.Memberships.FirstOrDefault()?.Role))
            .ToList();

        return ApiResponse<List<ClinicResponseDto>>.Success(response);
    }

    public async Task<ApiResponse<ClinicResponseDto>> GetClinicByIdAsync(
        Guid currentUserId,
        Guid clinicId)
    {
        var clinic = await _dbContext.Clinics
            .AsNoTracking()
            .Where(clinic => clinic.Id == clinicId)
            .Where(clinic => clinic.Memberships.Any(membership =>
                membership.UserId == currentUserId
                && membership.IsActive
                && membership.User.IsActive))
            .FirstOrDefaultAsync();

        return clinic is null
            ? ApiResponse<ClinicResponseDto>.Error(404, "Clinic not found")
            : ApiResponse<ClinicResponseDto>.Success(ClinicMapper.ToResponseDto(clinic));
    }

    public async Task<ApiResponse<ClinicResponseDto>> CreateClinicAsync(
        Guid currentUserId,
        CreateClinicRequestDto request)
    {
        var userExists = await _dbContext.Users
            .AnyAsync(user => user.Id == currentUserId && user.IsActive);

        if (!userExists)
        {
            return ApiResponse<ClinicResponseDto>.Error(401, "User not authenticated");
        }

        var clinic = new Domain.Entities.Clinic.Clinic(
            request.Name,
            AddressMapper.ToAddress(request.Address),
            request.OpenTime,
            request.CloseTime,
            request.TimeZoneId);

        clinic.UpdateDetails(
            request.Name,
            AddressMapper.ToAddress(request.Address),
            request.OpenTime,
            request.CloseTime,
            request.PhoneNumber,
            request.PhoneNumber2,
            request.PhoneNumber3,
            request.Email,
            request.Website,
            request.TimeZoneId);

        _dbContext.Clinics.Add(clinic);
        _dbContext.ClinicMemberships.Add(
            new ClinicMembership(currentUserId, clinic.Id, ClinicRole.Owner));

        await _dbContext.SaveChangesAsync();

        return ApiResponse<ClinicResponseDto>.Success(
            ClinicMapper.ToResponseDto(clinic),
            "Clinic created",
            201);
    }

    public async Task<ApiResponse<ClinicResponseDto>> UpdateClinicAsync(
        Guid currentUserId,
        Guid clinicId,
        UpdateClinicRequestDto request)
    {
        var membership = await ClinicAccess.GetMembershipAsync(
            _dbContext,
            currentUserId,
            clinicId);

        if (membership is null)
        {
            return ApiResponse<ClinicResponseDto>.Error(403, "You are not a member of this clinic");
        }

        if (!ClinicAccess.CanManage(membership.Role))
        {
            return ApiResponse<ClinicResponseDto>.Error(403, "You are not allowed to update this clinic");
        }

        await using var transaction = await ClinicTransactionLock.BeginAsync(_dbContext, clinicId);

        var clinic = await _dbContext.Clinics.FirstOrDefaultAsync(
            candidate => candidate.Id == clinicId);

        if (clinic is null)
        {
            return ApiResponse<ClinicResponseDto>.Error(404, "Clinic not found");
        }

        var scheduleOutsideNewHours = await _dbContext.DoctorWeeklyAvailabilities.AnyAsync(
            availability => availability.ClinicId == clinicId
                && (availability.StartTime < request.OpenTime
                    || availability.EndTime > request.CloseTime));
        if (scheduleOutsideNewHours)
        {
            return ApiResponse<ClinicResponseDto>.Error(
                409,
                "Clinic hours cannot be changed while doctor availability falls outside the new hours.");
        }

        if (request.TimeZoneId is not null
            && !string.Equals(request.TimeZoneId, clinic.TimeZoneId, StringComparison.Ordinal)
            && await _dbContext.Appointments.AnyAsync(appointment => appointment.ClinicId == clinicId))
        {
            return ApiResponse<ClinicResponseDto>.Error(
                409,
                "Clinic time zone cannot be changed after an appointment has been created.");
        }

        clinic.UpdateDetails(
            request.Name,
            AddressMapper.ToAddress(request.Address),
            request.OpenTime,
            request.CloseTime,
            request.PhoneNumber,
            request.PhoneNumber2,
            request.PhoneNumber3,
            request.Email,
            request.Website,
            request.TimeZoneId);

        await _dbContext.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return ApiResponse<ClinicResponseDto>.Success(ClinicMapper.ToResponseDto(clinic));
    }
}
