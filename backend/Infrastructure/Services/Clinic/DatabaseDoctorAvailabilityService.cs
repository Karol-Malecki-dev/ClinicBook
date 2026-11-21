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
    private const string AvailabilityIndexName = "IX_DoctorAvail_Clinic_Doctor_Day_Start";

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

        await using var transaction = await ClinicTransactionLock.BeginAsync(_dbContext, clinicId);

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
            if (transaction is not null)
            {
                await transaction.CommitAsync();
            }
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

        await using var transaction = await ClinicTransactionLock.BeginAsync(_dbContext, clinicId);

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

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(clinic!.TimeZoneId);
        if (await WouldWeeklyChangeUncoverBookingAsync(
                availability,
                timeZone,
                request.DayOfWeek,
                request.StartTime,
                request.EndTime,
                request.SlotDurationMinutes))
        {
            return ApiResponse<DoctorWeeklyAvailabilityDto>.Error(
                409,
                "The schedule change conflicts with an existing booked appointment.");
        }

        availability.Update(
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            request.SlotDurationMinutes);

        try
        {
            await _dbContext.SaveChangesAsync();
            if (transaction is not null)
            {
                await transaction.CommitAsync();
            }
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
        await using var transaction = await ClinicTransactionLock.BeginAsync(_dbContext, clinicId);

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

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(clinic!.TimeZoneId);
        if (await WouldWeeklyChangeUncoverBookingAsync(availability, timeZone))
        {
            return ApiResponse<bool>.Error(
                409,
                "The availability cannot be removed because it contains a booked appointment.");
        }

        _dbContext.DoctorWeeklyAvailabilities.Remove(availability);
        await _dbContext.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return ApiResponse<bool>.Success(true, "Doctor availability deleted.");
    }

    public async Task<ApiResponse<List<DoctorScheduleExceptionDto>>> GetScheduleExceptionsAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid doctorUserId)
    {
        var (clinic, doctor, error) = await GetDoctorContextAsync(clinicId, doctorUserId);
        if (error is not null)
        {
            return ApiResponse<List<DoctorScheduleExceptionDto>>.Error(
                error.Value.StatusCode,
                error.Value.Message);
        }

        if (!await CanManageScheduleAsync(currentUserId, clinicId, doctorUserId))
        {
            return ApiResponse<List<DoctorScheduleExceptionDto>>.Error(
                403,
                "You cannot manage this doctor's schedule.");
        }

        var items = await _dbContext.DoctorScheduleExceptions
            .AsNoTracking()
            .Where(item => item.ClinicId == clinic!.Id && item.DoctorProfileId == doctor!.Id)
            .OrderBy(item => item.Date)
            .ThenBy(item => item.StartTime)
            .ToListAsync();

        return ApiResponse<List<DoctorScheduleExceptionDto>>.Success(
            items.Select(item => ToDto(item, doctorUserId)).ToList());
    }

    public async Task<ApiResponse<DoctorScheduleExceptionDto>> CreateScheduleExceptionAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid doctorUserId,
        CreateDoctorScheduleExceptionRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var transaction = await ClinicTransactionLock.BeginAsync(
            _dbContext,
            clinicId);

        var (clinic, doctor, error) = await GetDoctorContextAsync(clinicId, doctorUserId);
        if (error is not null)
        {
            return ApiResponse<DoctorScheduleExceptionDto>.Error(
                error.Value.StatusCode,
                error.Value.Message);
        }

        if (!await CanManageScheduleAsync(currentUserId, clinicId, doctorUserId))
        {
            return ApiResponse<DoctorScheduleExceptionDto>.Error(
                403,
                "You cannot manage this doctor's schedule.");
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(clinic!.TimeZoneId);
        var todayAtClinic = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
        if (request.Date < todayAtClinic)
        {
            return ApiResponse<DoctorScheduleExceptionDto>.Error(
                400,
                "Schedule exception date cannot be in the past.");
        }

        if (request.StartTime < clinic.OpenTime || request.EndTime > clinic.CloseTime)
        {
            return ApiResponse<DoctorScheduleExceptionDto>.Error(
                400,
                "Schedule exception must fit within clinic opening hours.");
        }

        if (!TryGetUtcRange(
                request.Date,
                request.StartTime,
                request.EndTime,
                timeZone,
                out var startsAtUtc,
                out var endsAtUtc))
        {
            return ApiResponse<DoctorScheduleExceptionDto>.Error(
                400,
                "Schedule exception time cannot be represented unambiguously.");
        }

        var overlappingException = await _dbContext.DoctorScheduleExceptions
            .AnyAsync(item =>
                item.ClinicId == clinic.Id
                && item.DoctorProfileId == doctor!.Id
                && item.Date == request.Date
                && (item.Type == request.Type
                    || item.Type == ScheduleExceptionType.Closed
                    || request.Type == ScheduleExceptionType.Closed)
                && request.StartTime < item.EndTime
                && item.StartTime < request.EndTime);
        if (overlappingException)
        {
            return ApiResponse<DoctorScheduleExceptionDto>.Error(
                409,
                "This schedule exception overlaps an existing exception.");
        }

        if (request.Type == ScheduleExceptionType.Closed
            && await HasBookedAppointmentOverlapAsync(
                clinic.Id,
                doctor!.Id,
                startsAtUtc,
                endsAtUtc))
        {
            return ApiResponse<DoctorScheduleExceptionDto>.Error(
                409,
                "The exception conflicts with an existing booked appointment.");
        }

        var scheduleException = new DoctorScheduleException(
            clinic.Id,
            doctor!.Id,
            request.Date,
            request.Type,
            request.StartTime,
            request.EndTime,
            request.SlotDurationMinutes,
            request.Reason);

        _dbContext.DoctorScheduleExceptions.Add(scheduleException);
        await _dbContext.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return ApiResponse<DoctorScheduleExceptionDto>.Success(
            ToDto(scheduleException, doctorUserId),
            "Doctor schedule exception created.",
            201);
    }

    public async Task<ApiResponse<bool>> DeleteScheduleExceptionAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid doctorUserId,
        Guid exceptionId)
    {
        await using var transaction = await ClinicTransactionLock.BeginAsync(
            _dbContext,
            clinicId);

        var (clinic, doctor, error) = await GetDoctorContextAsync(clinicId, doctorUserId);
        if (error is not null)
        {
            return ApiResponse<bool>.Error(error.Value.StatusCode, error.Value.Message);
        }

        if (!await CanManageScheduleAsync(currentUserId, clinicId, doctorUserId))
        {
            return ApiResponse<bool>.Error(
                403,
                "You cannot manage this doctor's schedule.");
        }

        var scheduleException = await _dbContext.DoctorScheduleExceptions
            .FirstOrDefaultAsync(item =>
                item.Id == exceptionId
                && item.ClinicId == clinic!.Id
                && item.DoctorProfileId == doctor!.Id);
        if (scheduleException is null)
        {
            return ApiResponse<bool>.Error(404, "Schedule exception not found.");
        }

        if (scheduleException.Type == ScheduleExceptionType.Available)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(clinic!.TimeZoneId);
            if (!TryGetUtcRange(
                    scheduleException.Date,
                    scheduleException.StartTime,
                    scheduleException.EndTime,
                    timeZone,
                    out var startsAtUtc,
                    out var endsAtUtc))
            {
                return ApiResponse<bool>.Error(
                    409,
                    "Schedule exception cannot be represented unambiguously and must be handled carefully.");
            }

            if (await HasBookingOnlyCoveredByExceptionAsync(
                    doctor!,
                    scheduleException,
                    timeZone,
                    startsAtUtc,
                    endsAtUtc))
            {
                return ApiResponse<bool>.Error(
                    409,
                    "The availability cannot be removed because it contains a booked appointment.");
            }
        }

        _dbContext.DoctorScheduleExceptions.Remove(scheduleException);
        await _dbContext.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return ApiResponse<bool>.Success(true, "Doctor schedule exception deleted.");
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

        var exceptions = await _dbContext.DoctorScheduleExceptions
            .AsNoTracking()
            .Where(item =>
                item.ClinicId == clinic.Id
                && item.DoctorProfileId == doctor!.Id
                && item.Date == date)
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;
        var slots = GenerateSlots(date, weeklyAvailability, exceptions, timeZone, now);
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
        IReadOnlyCollection<DoctorScheduleException> exceptions,
        TimeZoneInfo timeZone,
        DateTimeOffset now)
    {
        var slots = new List<AvailabilitySlotDto>();
        foreach (var schedule in schedules)
        {
            AddSlots(
                slots,
                date,
                schedule.StartTime,
                schedule.EndTime,
                schedule.SlotDurationMinutes,
                timeZone,
                now);
        }

        foreach (var scheduleException in exceptions.Where(item =>
                     item.Type == ScheduleExceptionType.Available))
        {
            AddSlots(
                slots,
                date,
                scheduleException.StartTime,
                scheduleException.EndTime,
                scheduleException.SlotDurationMinutes,
                timeZone,
                now);
        }

        var closures = exceptions
            .Where(item => item.Type == ScheduleExceptionType.Closed)
            .ToList();

        return slots
            .Where(slot => closures.All(closure =>
            {
                var localStart = TimeOnly.FromDateTime(slot.LocalStart);
                var localEnd = TimeOnly.FromDateTime(slot.LocalEnd);
                return localStart >= closure.EndTime || localEnd <= closure.StartTime;
            }))
            .GroupBy(slot => slot.StartsAtUtc)
            .Select(group => group.First())
            .OrderBy(slot => slot.StartsAtUtc)
            .ToList();
    }

    private static void AddSlots(
        ICollection<AvailabilitySlotDto> slots,
        DateOnly date,
        TimeOnly rangeStart,
        TimeOnly rangeEnd,
        int slotDurationMinutes,
        TimeZoneInfo timeZone,
        DateTimeOffset now)
    {
        var startTime = rangeStart;
        var slotDuration = TimeSpan.FromMinutes(slotDurationMinutes);
        while (rangeEnd - startTime >= slotDuration)
        {
            var endTime = startTime.AddMinutes(slotDurationMinutes);
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

    private static DoctorScheduleExceptionDto ToDto(
        DoctorScheduleException scheduleException,
        Guid doctorUserId)
    {
        return new DoctorScheduleExceptionDto
        {
            Id = scheduleException.Id,
            ClinicId = scheduleException.ClinicId,
            DoctorProfileId = scheduleException.DoctorProfileId,
            DoctorUserId = doctorUserId,
            Date = scheduleException.Date,
            Type = scheduleException.Type,
            StartTime = scheduleException.StartTime,
            EndTime = scheduleException.EndTime,
            SlotDurationMinutes = scheduleException.SlotDurationMinutes,
            Reason = scheduleException.Reason
        };
    }

    private Task<bool> HasBookedAppointmentOverlapAsync(
        Guid clinicId,
        Guid doctorProfileId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc)
    {
        return _dbContext.Appointments.AnyAsync(appointment =>
            appointment.DoctorProfileId == doctorProfileId
            && appointment.ClinicId == clinicId
            && appointment.Status == AppointmentStatus.Booked
            && appointment.StartsAtUtc < endsAtUtc
            && appointment.EndsAtUtc > startsAtUtc);
    }

    private async Task<bool> HasBookingOnlyCoveredByExceptionAsync(
        DoctorProfile doctor,
        DoctorScheduleException scheduleException,
        TimeZoneInfo timeZone,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc)
    {
        var bookings = await _dbContext.Appointments
            .AsNoTracking()
            .Where(appointment =>
                appointment.DoctorProfileId == doctor.Id
                && appointment.ClinicId == scheduleException.ClinicId
                && appointment.Status == AppointmentStatus.Booked
                && appointment.StartsAtUtc < endsAtUtc
                && appointment.EndsAtUtc > startsAtUtc)
            .ToListAsync();
        if (bookings.Count == 0)
        {
            return false;
        }

        var weeklyAvailability = await _dbContext.DoctorWeeklyAvailabilities
            .AsNoTracking()
            .Where(item =>
                item.ClinicId == scheduleException.ClinicId
                && item.DoctorProfileId == doctor.Id
                && item.DayOfWeek == scheduleException.Date.DayOfWeek)
            .ToListAsync();

        return bookings.Any(booking =>
        {
            var localStart = TimeZoneInfo.ConvertTime(booking.StartsAtUtc, timeZone);
            var localEnd = TimeZoneInfo.ConvertTime(booking.EndsAtUtc, timeZone);
            if (DateOnly.FromDateTime(localStart.DateTime) != scheduleException.Date
                || DateOnly.FromDateTime(localEnd.DateTime) != scheduleException.Date)
            {
                return true;
            }

            var startTime = TimeOnly.FromDateTime(localStart.DateTime);
            var endTime = TimeOnly.FromDateTime(localEnd.DateTime);
            var coveredByWeeklySchedule = weeklyAvailability.Any(schedule =>
            {
                if (startTime < schedule.StartTime || endTime > schedule.EndTime)
                {
                    return false;
                }

                var offsetMinutes = (startTime - schedule.StartTime).TotalMinutes;
                var durationMinutes = (endTime - startTime).TotalMinutes;
                return offsetMinutes % schedule.SlotDurationMinutes == 0
                    && durationMinutes == schedule.SlotDurationMinutes;
            });

            return !coveredByWeeklySchedule;
        });
    }

    private async Task<bool> WouldWeeklyChangeUncoverBookingAsync(
        DoctorWeeklyAvailability changedAvailability,
        TimeZoneInfo timeZone,
        DayOfWeek? replacementDay = null,
        TimeOnly? replacementStart = null,
        TimeOnly? replacementEnd = null,
        int? replacementSlotDuration = null)
    {
        var now = DateTimeOffset.UtcNow;
        var bookings = await _dbContext.Appointments
            .AsNoTracking()
            .Where(appointment =>
                appointment.DoctorProfileId == changedAvailability.DoctorProfileId
                && appointment.ClinicId == changedAvailability.ClinicId
                && appointment.Status == AppointmentStatus.Booked
                && appointment.StartsAtUtc > now)
            .ToListAsync();
        if (bookings.Count == 0)
        {
            return false;
        }

        var otherAvailability = await _dbContext.DoctorWeeklyAvailabilities
            .AsNoTracking()
            .Where(item =>
                item.ClinicId == changedAvailability.ClinicId
                && item.DoctorProfileId == changedAvailability.DoctorProfileId
                && item.Id != changedAvailability.Id)
            .ToListAsync();
        var additionalAvailability = await _dbContext.DoctorScheduleExceptions
            .AsNoTracking()
            .Where(item =>
                item.ClinicId == changedAvailability.ClinicId
                && item.DoctorProfileId == changedAvailability.DoctorProfileId
                && item.Type == ScheduleExceptionType.Available
                && item.Date >= DateOnly.FromDateTime(
                    TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime))
            .ToListAsync();

        return bookings.Any(booking =>
        {
            var localStart = TimeZoneInfo.ConvertTime(booking.StartsAtUtc, timeZone);
            var localEnd = TimeZoneInfo.ConvertTime(booking.EndsAtUtc, timeZone);
            if (DateOnly.FromDateTime(localStart.DateTime)
                != DateOnly.FromDateTime(localEnd.DateTime))
            {
                return false;
            }

            var date = DateOnly.FromDateTime(localStart.DateTime);
            var startTime = TimeOnly.FromDateTime(localStart.DateTime);
            var endTime = TimeOnly.FromDateTime(localEnd.DateTime);
            if (!IsCoveredByWindow(
                    date.DayOfWeek,
                    startTime,
                    endTime,
                    changedAvailability.DayOfWeek,
                    changedAvailability.StartTime,
                    changedAvailability.EndTime,
                    changedAvailability.SlotDurationMinutes))
            {
                return false;
            }

            var remainsCovered = otherAvailability.Any(item => IsCoveredByWindow(
                date.DayOfWeek,
                startTime,
                endTime,
                item.DayOfWeek,
                item.StartTime,
                item.EndTime,
                item.SlotDurationMinutes));

            remainsCovered |= additionalAvailability.Any(item =>
                item.Date == date
                && IsCoveredByWindow(
                    date.DayOfWeek,
                    startTime,
                    endTime,
                    date.DayOfWeek,
                    item.StartTime,
                    item.EndTime,
                    item.SlotDurationMinutes));

            if (!remainsCovered
                && replacementDay.HasValue
                && replacementStart.HasValue
                && replacementEnd.HasValue
                && replacementSlotDuration.HasValue)
            {
                remainsCovered = IsCoveredByWindow(
                    date.DayOfWeek,
                    startTime,
                    endTime,
                    replacementDay.Value,
                    replacementStart.Value,
                    replacementEnd.Value,
                    replacementSlotDuration.Value);
            }

            return !remainsCovered;
        });
    }

    private static bool IsCoveredByWindow(
        DayOfWeek dateDay,
        TimeOnly appointmentStart,
        TimeOnly appointmentEnd,
        DayOfWeek windowDay,
        TimeOnly windowStart,
        TimeOnly windowEnd,
        int slotDurationMinutes)
    {
        if (dateDay != windowDay
            || appointmentStart < windowStart
            || appointmentEnd > windowEnd)
        {
            return false;
        }

        var offsetMinutes = (appointmentStart - windowStart).TotalMinutes;
        var durationMinutes = (appointmentEnd - appointmentStart).TotalMinutes;
        return offsetMinutes % slotDurationMinutes == 0
            && durationMinutes == slotDurationMinutes;
    }

    private static bool TryGetUtcRange(
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        TimeZoneInfo timeZone,
        out DateTimeOffset startsAtUtc,
        out DateTimeOffset endsAtUtc)
    {
        var localStart = date.ToDateTime(startTime, DateTimeKind.Unspecified);
        var localEnd = date.ToDateTime(endTime, DateTimeKind.Unspecified);
        if (!TryConvertLocalToUtc(localStart, timeZone, out startsAtUtc)
            || !TryConvertLocalToUtc(localEnd, timeZone, out endsAtUtc))
        {
            startsAtUtc = default;
            endsAtUtc = default;
            return false;
        }

        return true;
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
