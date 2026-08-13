using Application.DTOs.Clinic.ClinicMembership;
using Application.Interfaces.Clinic;
using Domain.Entities;
using Domain.Entities.Clinic;
using Domain.Enums;
using Infrastructure.Authorization;
using Infrastructure.Data;
using Infrastructure.Mappers.Clinic;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;

namespace Infrastructure.Services.Clinic;

public sealed class DatabaseClinicMembershipService : IClinicMembershipService
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseClinicMembershipService(ApplicationDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<ClinicMembershipResponseDto>> CreateClinicMembershipAsync(
        Guid currentUserId,
        Guid clinicId,
        AddClinicMembershipRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authorization = await GetManagementAuthorizationAsync(currentUserId, clinicId);
        if (authorization is not null)
        {
            return ApiResponse<ClinicMembershipResponseDto>.Error(authorization.Value.StatusCode, authorization.Value.Message);
        }

        if (!ClinicAccess.CanManageMembers(request.Role))
        {
            return ApiResponse<ClinicMembershipResponseDto>.Error(400, "Only the Patient or Doctor role can be assigned.");
        }

        var userExists = await _dbContext.Users.AnyAsync(user => user.Id == request.UserId && user.IsActive);
        if (!userExists)
        {
            return ApiResponse<ClinicMembershipResponseDto>.Error(404, "User not found or is not active.");
        }

        var membershipExists = await _dbContext.ClinicMemberships.AnyAsync(
            membership => membership.ClinicId == clinicId
                && membership.UserId == request.UserId
                && membership.IsActive);
        if (membershipExists)
        {
            return ApiResponse<ClinicMembershipResponseDto>.Error(400, "User is already an active member of the clinic.");
        }

        var membership = new ClinicMembership(request.UserId, clinicId, request.Role);
        _dbContext.ClinicMemberships.Add(membership);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<ClinicMembershipResponseDto>.Success(
            ClinicMembershipMapper.ToResponseDto(membership),
            "Clinic membership created successfully.",
            201);
    }

    public async Task<ApiResponse<bool>> DeleteClinicMembershipAsync(Guid currentUserId, Guid clinicId, Guid memberUserId)
    {
        var authorization = await GetManagementAuthorizationAsync(currentUserId, clinicId);
        if (authorization is not null)
        {
            return ApiResponse<bool>.Error(authorization.Value.StatusCode, authorization.Value.Message);
        }

        var membership = await _dbContext.ClinicMemberships.FirstOrDefaultAsync(
            item => item.ClinicId == clinicId
                && item.UserId == memberUserId
                && item.IsActive);

        if (membership is null)
        {
            return ApiResponse<bool>.Error(404, "Active clinic membership not found.");
        }

        if (!ClinicAccess.CanManageMembers(membership.Role))
        {
            return ApiResponse<bool>.Error(400, "Owner and ClinicAdministrator memberships cannot be deleted.");
        }

        membership.Deactivate();
        await _dbContext.SaveChangesAsync();

        return ApiResponse<bool>.Success(true, "Clinic membership deleted successfully.");
    }

    public async Task<ApiResponse<ClinicMembershipResponseDto>> GetClinicMembershipByUserIdAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid memberUserId)
    {
        var authorization = await GetMembershipAuthorizationAsync(currentUserId, clinicId);
        if (authorization is not null)
        {
            return ApiResponse<ClinicMembershipResponseDto>.Error(authorization.Value.StatusCode, authorization.Value.Message);
        }

        var membership = await _dbContext.ClinicMemberships.FirstOrDefaultAsync(
            item => item.ClinicId == clinicId
                && item.UserId == memberUserId
                && item.IsActive);

        return membership is null
            ? ApiResponse<ClinicMembershipResponseDto>.Error(404, "Active clinic membership not found.")
            : ApiResponse<ClinicMembershipResponseDto>.Success(ClinicMembershipMapper.ToResponseDto(membership), "Clinic membership retrieved successfully.");
    }

    public async Task<ApiResponse<List<ClinicMembershipResponseDto>>> GetClinicMembershipsAsync(
        Guid currentUserId,
        Guid clinicId)
    {
        var authorization = await GetMembershipAuthorizationAsync(currentUserId, clinicId);
        if (authorization is not null)
        {
            return ApiResponse<List<ClinicMembershipResponseDto>>.Error(authorization.Value.StatusCode, authorization.Value.Message);
        }

        var memberships = await _dbContext.ClinicMemberships
            .Where(item => item.ClinicId == clinicId && item.IsActive)
            .ToListAsync();

        return ApiResponse<List<ClinicMembershipResponseDto>>.Success(
            memberships.Select(ClinicMembershipMapper.ToResponseDto).ToList(),
            "Clinic memberships retrieved successfully.");
    }

    public async Task<ApiResponse<ClinicMembershipResponseDto>> UpdateClinicMembershipAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid memberUserId,
        UpdateClinicMembershipRoleRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authorization = await GetManagementAuthorizationAsync(currentUserId, clinicId);
        if (authorization is not null)
        {
            return ApiResponse<ClinicMembershipResponseDto>.Error(authorization.Value.StatusCode, authorization.Value.Message);
        }

        if (!ClinicAccess.CanManageMembers(request.Role))
        {
            return ApiResponse<ClinicMembershipResponseDto>.Error(400, "Only the Patient or Doctor role can be assigned.");
        }

        var membership = await _dbContext.ClinicMemberships.FirstOrDefaultAsync(
            item => item.ClinicId == clinicId
                && item.UserId == memberUserId
                && item.IsActive);

        if (membership is null)
        {
            return ApiResponse<ClinicMembershipResponseDto>.Error(404, "Active clinic membership not found.");
        }

        membership.ChangeRole(request.Role);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<ClinicMembershipResponseDto>.Success(
            ClinicMembershipMapper.ToResponseDto(membership),
            "Clinic membership updated successfully.");
    }

    private async Task<(int StatusCode, string Message)?> GetMembershipAuthorizationAsync(
        Guid currentUserId,
        Guid clinicId)
    {
        var membership = await ClinicAccess.GetMembershipAsync(_dbContext, currentUserId, clinicId);

        if (membership is null)
        {
            return (403, "Current user is not an active member of the clinic.");
        }

        return null;
    }

    private async Task<(int StatusCode, string Message)?> GetManagementAuthorizationAsync(
        Guid currentUserId,
        Guid clinicId)
    {
        var membership = await ClinicAccess.GetMembershipAsync(_dbContext, currentUserId, clinicId);

        if (membership is null)
        {
            return (403, "Current user is not an active member of the clinic.");
        }

        return ClinicAccess.CanManageMembers(membership.Role)
            ? null
            : (403, "Current user does not have permission to manage clinic members.");
    }
}
