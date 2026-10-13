using Application.DTOs.Doctor.DoctorSpecialization;
using Application.Interfaces.Doctor;
using Domain.Entities.Doctor;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Mappers.Doctor;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;

namespace Infrastructure.Services.Doctor;

public class DatabaseDoctorSpecializationService : IDoctorSpecializationService
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseDoctorSpecializationService(ApplicationDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<List<DoctorSpecializationResponseDto>>> GetDoctorSpecializationsAsync(Guid currentUserId, Guid doctorUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            return ApiResponse<List<DoctorSpecializationResponseDto>>.Error(400, "Current user ID cannot be empty.");
        }

        if(doctorUserId == Guid.Empty)
        {
            return ApiResponse<List<DoctorSpecializationResponseDto>>.Error(400, "Doctor user ID cannot be empty.");
        }

        var doctorSpecializations = await _dbContext.DoctorSpecializations
            .Where(ds => ds.DoctorProfile.UserId == doctorUserId)
            .ToListAsync();

        if(doctorSpecializations is null || !doctorSpecializations.Any())
        {
            return ApiResponse<List<DoctorSpecializationResponseDto>>.Error(404, "Doctor profile not found.");
        }

        var listOfDoctorSpecializations = DoctorSpecializationMapper.ToResponseListDto(doctorSpecializations);

        return ApiResponse<List<DoctorSpecializationResponseDto>>.Success(listOfDoctorSpecializations, "Doctor specializations retrieved successfully.");
    }
    public async Task<ApiResponse<DoctorSpecializationResponseDto>> AddDoctorSpecializationAsync(Guid currentUserId, Guid doctorUserId, AddDoctorSpecializationRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        if(currentUserId == Guid.Empty)
        {
            return ApiResponse<DoctorSpecializationResponseDto>.Error(400, "Current user ID cannot be empty.");
        }

        if(doctorUserId == Guid.Empty)
        {
            return ApiResponse<DoctorSpecializationResponseDto>.Error(400, "Doctor user ID cannot be empty.");
        }

        if(!await CanManageDoctorSpecializationsAsync(currentUserId, doctorUserId))
        {
            return ApiResponse<DoctorSpecializationResponseDto>.Error(403, "You do not have permission to manage this doctor's specializations.");
        }

        var doctorProfile = await _dbContext.DoctorProfiles
            .FirstOrDefaultAsync(dp => dp.UserId == doctorUserId);

        if (doctorProfile == null)
        {
            return ApiResponse<DoctorSpecializationResponseDto>.Error(404, "Doctor profile not found.");
        }

        var specialization = await _dbContext.Specializations
            .FirstOrDefaultAsync(s => s.Id == request.SpecializationId);

        if (specialization is null)
        {
            return ApiResponse<DoctorSpecializationResponseDto>.Error(404, "Specialization not found.");
        }

        var doctorSpecialization = new DoctorSpecialization(
            doctorProfile,
            specialization,
            request.ExperienceDescription);


        _dbContext.DoctorSpecializations.Add(doctorSpecialization);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<DoctorSpecializationResponseDto>.Success(
            DoctorSpecializationMapper.ToResponseDto(doctorSpecialization),
            "Doctor specialization added successfully.",
            201
        );

    }



    public async Task<ApiResponse<bool>> RemoveDoctorSpecializationAsync(Guid currentUserId, Guid doctorUserId, Guid doctorSpecializationId)
    {
        if (currentUserId == Guid.Empty)
        {
            return ApiResponse<bool>.Error(400, "Current user ID cannot be empty.");
        }

        if(doctorUserId == Guid.Empty)
        {
            return ApiResponse<bool>.Error(400, "Doctor user ID cannot be empty.");
        }

        if(doctorSpecializationId == Guid.Empty)
        {
            return ApiResponse<bool>.Error(400, "Doctor specialization ID cannot be empty.");
        }

        if (!await CanManageDoctorSpecializationsAsync(currentUserId, doctorUserId))
        {
            return ApiResponse<bool>.Error(403, "You do not have permission to manage this doctor's specializations.");
        }

        var doctorSpecialization = _dbContext.DoctorSpecializations.FirstOrDefault(ds => ds.Id == doctorSpecializationId && ds.DoctorProfile.UserId == doctorUserId);
        if (doctorSpecialization == null)
        {
            return ApiResponse<bool>.Error(404, "Doctor specialization not found.");
        }


        _dbContext.DoctorSpecializations.Remove(doctorSpecialization);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<bool>.Success(true, "Doctor specialization removed successfully.");
    }

    public async Task<ApiResponse<DoctorSpecializationResponseDto>> UpdateDoctorSpecializationAsync(Guid currentUserId, Guid doctorUserId, Guid doctorSpecializationId, UpdateDoctorSpecializationRequestDto request)
    {

        if (currentUserId == Guid.Empty)
        {
            return ApiResponse<DoctorSpecializationResponseDto>.Error(400, "Current user ID cannot be empty.");
        }

        if (doctorUserId == Guid.Empty)
        {
            return ApiResponse<DoctorSpecializationResponseDto>.Error(400, "Doctor user ID cannot be empty.");
        }

        if (doctorSpecializationId == Guid.Empty)
        {
            return ApiResponse<DoctorSpecializationResponseDto>.Error(400, "Doctor specialization ID cannot be empty.");
        }

        if (!await CanManageDoctorSpecializationsAsync(currentUserId, doctorUserId))
        {
            return ApiResponse<DoctorSpecializationResponseDto>.Error(403, "You do not have permission to manage this doctor's specializations.");
        }

        var doctorSpecialization = _dbContext.DoctorSpecializations.FirstOrDefault(ds => ds.Id == doctorSpecializationId && ds.DoctorProfile.UserId == doctorUserId);
        if (doctorSpecialization == null)
        {
            return ApiResponse<DoctorSpecializationResponseDto>.Error(404, "Doctor specialization not found.");
        }


        doctorSpecialization.UpdateExperienceDescription(request.ExperienceDescription);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<DoctorSpecializationResponseDto>.Success(
            DoctorSpecializationMapper.ToResponseDto(doctorSpecialization),
            "Doctor specialization updated successfully."
        );
    }

    public async Task<ApiResponse<bool>> DeleteDoctorSpecializationAsync(Guid currentUserId, Guid doctorUserId, Guid doctorSpecializationId)
    {
        if (currentUserId == Guid.Empty)
        {
            return ApiResponse<bool>.Error(400, "Current user ID cannot be empty.");
        }

        if (doctorUserId == Guid.Empty)
        {
            return ApiResponse<bool>.Error(400, "Doctor user ID cannot be empty.");
        }

        if (doctorSpecializationId == Guid.Empty)
        {
            return ApiResponse<bool>.Error(400, "Doctor specialization ID cannot be empty.");
        }

        if (!await CanManageDoctorSpecializationsAsync(currentUserId, doctorUserId))
        {
            return ApiResponse<bool>.Error(403, "You do not have permission to manage this doctor's specializations.");
        }

        var doctorSpecialization = _dbContext.DoctorSpecializations.FirstOrDefault(ds => ds.Id == doctorSpecializationId && ds.DoctorProfile.UserId == doctorUserId);
        if (doctorSpecialization == null)
        {
            return ApiResponse<bool>.Error(404, "Doctor specialization not found.");
        }

        _dbContext.DoctorSpecializations.Remove(doctorSpecialization);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<bool>.Success(true, "Doctor specialization deleted successfully.");
    }

    private async Task<bool> CanManageDoctorSpecializationsAsync(Guid currentUserId, Guid doctorUserId)
    {
        if (currentUserId == Guid.Empty || doctorUserId == Guid.Empty)
        {
            return false;
        }

        return await _dbContext.ClinicMemberships
            .AsNoTracking()
            .AnyAsync(currentMemebrship =>
                currentMemebrship.UserId == currentUserId &&
                currentMemebrship.IsActive &&
                currentMemebrship.User.IsActive &&
                (currentMemebrship.Role == ClinicRole.ClinicAdministrator || currentMemebrship.Role == ClinicRole.Owner) &&
                _dbContext.ClinicMemberships.Any(doctoMembership =>
                doctoMembership.UserId == doctorUserId &&
                doctoMembership.ClinicId == currentMemebrship.ClinicId &&
                doctoMembership.IsActive &&
                doctoMembership.User.IsActive &&
                doctoMembership.Role == ClinicRole.Doctor));
    }
}