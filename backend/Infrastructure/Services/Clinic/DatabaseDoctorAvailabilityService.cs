using Application.DTOs.Clinic;
using Application.Interfaces.Clinic;
using Domain.Entities.Clinic;
using Domain.Entities.Doctor;
using Domain.Enums;
using Infrastructure.Authorization;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Responses;

namespace Infrastructure.Services.Clinic;

public sealed class DatabaseDoctorAvailabilityService : IDoctorAvailabilityService
{
    private const string AvailabilityIndexName =
        "IX_DoctorWeeklyAvailabilities_ClinicId_DoctorProfileId_DayOfWeek_StartTime";

    private readonly ApplicationDbContext _dbContext;

    public DatabaseDoctorAvailabilityService(ApplicationDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<List<ClinicDoctorDto>>> GetClinicDoctorsAsync(
        Guid currentUserId,
        Guid clinicId)
    {
        if (currentUserId == Guid.Empty)
        {
            return ApiResponse<List<ClinicDoctorDto>>.Error(401, "User not authenticated.");
        }

        if (!await _dbContext.Clinics.AnyAsync(clinic => clinic.Id == clinicId))
        {
            return ApiResponse<List<ClinicDoctorDto>>.Error(404, "Clinic not found.");
        }

        var doctors = await _dbContext.DoctorProfiles
            .AsNoTracking()
            .Where(doctor => doctor.IsActive
                && doctor.User.IsActive
                && _dbContext.ClinicMemberships.Any(membership =>
                    membership.ClinicId == clinicId
                    && membership.UserId == doctor.UserId
                    && membership.Role == ClinicRole.Doctor
                    && membership.IsActive))
            .Include(doctor => doctor.DoctorSpecializations)
                .ThenInclude(doctorSpecialization => doctorSpecialization.Specialization)
            .OrderBy(doctor => doctor.LastName)
            .ThenBy(doctor => doctor.FirstName)
            .ToListAsync();

        var response = doctors.Select(doctor => new ClinicDoctorDto
        {
            DoctorProfileId = doctor.Id,
            UserId = doctor.UserId,
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            Bio = doctor.Bio,
            Specializations = doctor.DoctorSpecializations
                .Select(item => item.Specialization.Name)
                .OrderBy(name => name)
                .ToList()
        }).ToList();

        return ApiResponse<List<ClinicDoctorDto>>.Success(response);
    }

    public async Task<ApiResponse<List<DoctorWeeklyAvailabilityDto>>> GetWeeklyAvailabilityAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid doctorUserId)
    {
        var (clinic, doctor, error) = await GetDoctorContextAsync(clinicId, doctorUserId);
        if (error is not null)
        {
            return ApiResponse<List<DoctorWeeklyAvailabilityDto>>.Error(error.Value.StatusCode, error.Value.Message);
        }

        if (!await CanManageScheduleAsync(currentUserId, clinicId, doctorUserId))
        {
            return ApiResponse<List<DoctorWeeklyAvailabilityDto>>.Error(403, "You cannot manage this doctor's schedule.");
        }

        var availability = await _dbContext.DoctorWeeklyAvailabilities
            .AsNoTracking()
            .Where(item => item.ClinicId == clinic!.Id && item.DoctorProfileId == doctor!.Id)
            .OrderBy(item => item.DayOfWeek)
            .ThenBy(item => item.StartTime)
            .ToListAsync();

        return ApiResponse<List<DoctorWeeklyAvailabilityDto>>.Success(
            availability.Select(item => ToDto(item, doctorUserId)).ToList());
    }

    public async Task<ApiResponse<DoctorWeeklyAvailabilityDto>> CreateWeeklyAvailabilityAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid doctorUserId,
        SetDoctorWeeklyAvailabilityRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (clinic, doctor, error) = await GetDoctorContextAsync(clinicId, doctorUserId);
        if (error is not null)
        {
            return ApiResponse<DoctorWeeklyAvailabilityDto>.Error(error.Value.StatusCode, error.Value.Message);
        }

        if (!await CanManageScheduleAsync(currentUserId, clinicId, doctorUserId))
        {
            return ApiResponse<DoctorWeeklyAvailabilityDto>.Error(403, "You cannot manage this doctor's schedule.");
        }

        var windowError = ValidateWindow(clinic!, request);
        if (windowError is not null)
        {
            return ApiResponse<DoctorWeeklyAvailabilityDto>.Error(400, windowError);
        }

        if (await HasOverlappingWindowAsync(clinic!.Id, doctor!.Id, request))
        {
            return ApiResponse<DoctorWeeklyAvailabilityDto>.Error(409, "This availability overlaps another window.");
        }

        var availability = new DoctorWeeklyAvailability(
            clinic!.Id,
            doctor.Id,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            request.SlotDurationMinutes);

        _dbContext.DoctorWeeklyAvailabilities.Add(availability);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsAvailabilityUniqueViolation(exception))
        {
            return ApiResponse<DoctorWeeklyAvailabilityDto>.Error(409, "This availability already exists.");
        }

        return ApiResponse<DoctorWeeklyAvailabilityDto>.Success(
            ToDto(availability, doctorUserId),
            "Doctor availability created.",
            201);
    }

    public async Task<ApiResponse<DoctorWeeklyAvailabilityDto>> UpdateWeeklyAvailabilityAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid doctorUserId,
        Guid availabilityId,
        SetDoctorWeeklyAvailabilityRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (clinic, doctor, error) = await GetDoctorContextAsync(clinicId, doctorUserId);
        if (error is not null)
        {
            return ApiResponse<DoctorWeeklyAvailabilityDto>.Error(error.Value.StatusCode, error.Value.Message);
        }

        if (!await CanManageScheduleAsync(currentUserId, clinicId, doctorUserId))
        {
            return ApiResponse<DoctorWeeklyAvailabilityDto>.Error(403, "You cannot manage this doctor's schedule.");
        }

        var availability = await _dbContext.DoctorWeeklyAvailabilities.FirstOrDefaultAsync(item =>
            item.Id == availabilityId
            && item.ClinicId == clinic!.Id
            && item.DoctorProfileId == doctor!.Id);

        if (availability is null)
        {
            return ApiResponse<DoctorWeeklyAvailabilityDto>.Error(404, "Availability window not found.");
        }

        var windowError = ValidateWindow(clinic!, request);
        if (windowError is not null)
        {
            return ApiResponse<DoctorWeeklyAvailabilityDto>.Error(400, windowError);
        }

        if (await HasOverlappingWindowAsync(clinic!.Id, doctor!.Id, request, availabilityId))
        {
            return ApiResponse<DoctorWeeklyAvailabilityDto>.Error(409, "This availability overlaps another window.");
        }

        availability.Update(
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            request.SlotDurationMinutes);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsAvailabilityUniqueViolation(exception))
        {
            return ApiResponse<DoctorWeeklyAvailabilityDto>.Error(409, "This availability already exists.");
        }

        return ApiResponse<DoctorWeeklyAvailabilityDto>.Success(
            ToDto(availability, doctorUserId),
            "Doctor availability updated.");
    }

    public async Task<ApiResponse<bool>> DeleteWeeklyAvailabilityAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid doctorUserId,
        Guid availabilityId)
    {
        var (clinic, doctor, error) = await GetDoctorContextAsync(clinicId, doctorUserId);
        if (error is not null)
        {
            return ApiResponse<bool>.Error(error.Value.StatusCode, error.Value.Message);
        }

        if (!await CanManageScheduleAsync(currentUserId, clinicId, doctorUserId))
        {
            return ApiResponse<bool>.Error(403, "You cannot manage this doctor's schedule.");
        }

        var availability = await _dbContext.DoctorWeeklyAvailabilities.FirstOrDefaultAsync(item =>
            item.Id == availabilityId
            && item.ClinicId == clinic!.Id
            && item.DoctorProfileId == doctor!.Id);

        if (availability is null)
        {
            return ApiResponse<bool>.Error(404, "Availability window not found.");
        }

        _dbContext.DoctorWeeklyAvailabilities.Remove(availability);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<bool>.Success(true, "Doctor availability deleted.");
    }

    public async Task<ApiResponse<List<AvailabilitySlotDto>>> GetAvailableSlotsAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid doctorUserId,
        DateOnly date)
    {
        if (currentUserId == Guid.Empty)
        {
            return ApiResponse<List<AvailabilitySlotDto>>.Error(401, "User not authenticated.");
        }

        var (clinic, doctor, error) = await GetDoctorContextAsync(clinicId, doctorUserId);
        if (error is not null)
        {
            return ApiResponse<List<AvailabilitySlotDto>>.Error(error.Value.StatusCode, error.Value.Message);
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(clinic!.TimeZoneId);
        var todayAtClinic = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
        if (date < todayAtClinic)
        {
            return ApiResponse<List<AvailabilitySlotDto>>.Error(400, "Availability date cannot be in the past.");
        }

        var weeklyAvailability = await _dbContext.DoctorWeeklyAvailabilities
            .AsNoTracking()
            .Where(item =>
                item.ClinicId == clinic.Id
                && item.DoctorProfileId == doctor!.Id
                && item.DayOfWeek == date.DayOfWeek)
            .OrderBy(item => item.StartTime)
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;
        var slots = GenerateSlots(date, weeklyAvailability, timeZone, now);
        if (slots.Count == 0)
        {
            return ApiResponse<List<AvailabilitySlotDto>>.Success(slots);
        }

        var earliestStart = slots[0].StartsAtUtc;
        var latestEnd = slots[^1].EndsAtUtc;
        var bookedAppointments = await _dbContext.Appointments
            .AsNoTracking()
            .Where(appointment =>
                appointment.DoctorProfileId == doctor!.Id
                && appointment.Status == AppointmentStatus.Booked
                && appointment.StartsAtUtc < latestEnd
                && appointment.EndsAtUtc > earliestStart)
            .Select(appointment => new
            {
                appointment.StartsAtUtc,
                appointment.EndsAtUtc
            })
            .ToListAsync();

        var availableSlots = slots
            .Where(slot => bookedAppointments.All(appointment =>
                appointment.StartsAtUtc >= slot.EndsAtUtc
                || appointment.EndsAtUtc <= slot.StartsAtUtc))
            .ToList();

        return ApiResponse<List<AvailabilitySlotDto>>.Success(availableSlots);
    }

    private async Task<(Domain.Entities.Clinic.Clinic? Clinic, DoctorProfile? Doctor, (int StatusCode, string Message)? Error)>
        GetDoctorContextAsync(Guid clinicId, Guid doctorUserId)
    {
        var clinic = await _dbContext.Clinics.FirstOrDefaultAsync(item => item.Id == clinicId);
        if (clinic is null)
        {
            return (null, null, (404, "Clinic not found."));
        }

        var doctor = await _dbContext.DoctorProfiles.FirstOrDefaultAsync(item =>
            item.UserId == doctorUserId
            && item.IsActive
            && item.User.IsActive);
        if (doctor is null)
        {
            return (clinic, null, (404, "Active doctor profile not found."));
        }

        var doctorMembership = await ClinicAccess.GetMembershipAsync(
            _dbContext,
            doctorUserId,
            clinicId);
        if (doctorMembership?.Role != ClinicRole.Doctor)
        {
            return (clinic, null, (404, "Doctor is not an active member of this clinic."));
        }

        return (clinic, doctor, null);
    }

    private async Task<bool> CanManageScheduleAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid doctorUserId)
    {
        var membership = await ClinicAccess.GetMembershipAsync(
            _dbContext,
            currentUserId,
            clinicId);

        if (membership is null)
        {
            return false;
        }

        return ClinicAccess.CanManageDoctors(membership.Role)
            || (currentUserId == doctorUserId && membership.Role == ClinicRole.Doctor);
    }

    private async Task<bool> HasOverlappingWindowAsync(
        Guid clinicId,
        Guid doctorProfileId,
        SetDoctorWeeklyAvailabilityRequestDto request,
        Guid? excludedAvailabilityId = null)
    {
        return await _dbContext.DoctorWeeklyAvailabilities.AnyAsync(existing =>
            existing.ClinicId == clinicId
            && existing.DoctorProfileId == doctorProfileId
            && existing.DayOfWeek == request.DayOfWeek
            && existing.Id != excludedAvailabilityId
            && request.StartTime < existing.EndTime
            && existing.StartTime < request.EndTime);
    }

    private static string? ValidateWindow(
        Domain.Entities.Clinic.Clinic clinic,
        SetDoctorWeeklyAvailabilityRequestDto request)
    {
        return request.StartTime < clinic.OpenTime || request.EndTime > clinic.CloseTime
            ? "Doctor availability must fit within clinic opening hours."
            : null;
    }

    private static List<AvailabilitySlotDto> GenerateSlots(
        DateOnly date,
        IReadOnlyCollection<DoctorWeeklyAvailability> schedules,
        TimeZoneInfo timeZone,
        DateTimeOffset now)
    {
        var slots = new List<AvailabilitySlotDto>();
        foreach (var schedule in schedules)
        {
            var startTime = schedule.StartTime;
            while (startTime.AddMinutes(schedule.SlotDurationMinutes) <= schedule.EndTime)
            {
                var endTime = startTime.AddMinutes(schedule.SlotDurationMinutes);
                var localStart = date.ToDateTime(startTime, DateTimeKind.Unspecified);
                var localEnd = date.ToDateTime(endTime, DateTimeKind.Unspecified);

                if (TryConvertLocalToUtc(localStart, timeZone, out var startsAtUtc)
                    && TryConvertLocalToUtc(localEnd, timeZone, out var endsAtUtc)
                    && startsAtUtc > now)
                {
                    slots.Add(new AvailabilitySlotDto
                    {
                        StartsAtUtc = startsAtUtc,
                        EndsAtUtc = endsAtUtc,
                        LocalStart = localStart,
                        LocalEnd = localEnd,
                        TimeZoneId = timeZone.Id
                    });
                }

                startTime = endTime;
            }
        }

        return slots.OrderBy(slot => slot.StartsAtUtc).ToList();
    }

    private static bool TryConvertLocalToUtc(
        DateTime localTime,
        TimeZoneInfo timeZone,
        out DateTimeOffset utcTime)
    {
        if (timeZone.IsInvalidTime(localTime) || timeZone.IsAmbiguousTime(localTime))
        {
            utcTime = default;
            return false;
        }

        utcTime = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localTime, timeZone));
        return true;
    }

    private static DoctorWeeklyAvailabilityDto ToDto(
        DoctorWeeklyAvailability availability,
        Guid doctorUserId)
    {
        return new DoctorWeeklyAvailabilityDto
        {
            Id = availability.Id,
            ClinicId = availability.ClinicId,
            DoctorProfileId = availability.DoctorProfileId,
            DoctorUserId = doctorUserId,
            DayOfWeek = availability.DayOfWeek,
            StartTime = availability.StartTime,
            EndTime = availability.EndTime,
            SlotDurationMinutes = availability.SlotDurationMinutes
        };
    }

    private static bool IsAvailabilityUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: AvailabilityIndexName
        };
    }
}
