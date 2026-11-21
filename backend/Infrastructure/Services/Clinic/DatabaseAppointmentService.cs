using Application.DTOs.Clinic;
using Application.Interfaces.Clinic;
using Domain.Entities.Clinic;
using Domain.Enums;
using Infrastructure.Authorization;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Responses;
using System.Data;

namespace Infrastructure.Services.Clinic;

public sealed class DatabaseAppointmentService : IAppointmentService
{
    private const string ActiveBookingIndexName =
        "IX_Appointments_DoctorProfileId_StartsAtUtc_Booked";

    private readonly ApplicationDbContext _dbContext;
    private readonly IDoctorAvailabilityService _availabilityService;

    public DatabaseAppointmentService(
        ApplicationDbContext dbContext,
        IDoctorAvailabilityService availabilityService)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(availabilityService);
        _dbContext = dbContext;
        _availabilityService = availabilityService;
    }

    public async Task<ApiResponse<AppointmentResponseDto>> BookAppointmentAsync(
        Guid currentUserId,
        Guid clinicId,
        Guid doctorUserId,
        CreateAppointmentRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (currentUserId == Guid.Empty
            || !await _dbContext.Users.AnyAsync(user => user.Id == currentUserId && user.IsActive))
        {
            return ApiResponse<AppointmentResponseDto>.Error(401, "User not authenticated.");
        }

        try
        {
            await using var transaction = await ClinicTransactionLock.BeginAsync(
                _dbContext,
                clinicId,
                IsolationLevel.Serializable);

            var clinic = await _dbContext.Clinics.FirstOrDefaultAsync(item => item.Id == clinicId);
            if (clinic is null)
            {
                return ApiResponse<AppointmentResponseDto>.Error(404, "Clinic not found.");
            }

            var doctor = await _dbContext.DoctorProfiles.FirstOrDefaultAsync(item =>
                item.UserId == doctorUserId
                && item.IsActive
                && item.User.IsActive);
            if (doctor is null)
            {
                return ApiResponse<AppointmentResponseDto>.Error(404, "Active doctor profile not found.");
            }

            var startUtc = request.StartsAtUtc.ToUniversalTime();
            var clinicTimeZone = TimeZoneInfo.FindSystemTimeZoneById(clinic.TimeZoneId);
            var localStart = TimeZoneInfo.ConvertTime(startUtc, clinicTimeZone).DateTime;
            var localDate = DateOnly.FromDateTime(localStart);

            var slotsResponse = await _availabilityService.GetAvailableSlotsAsync(
                currentUserId,
                clinicId,
                doctorUserId,
                localDate);

            if (slotsResponse.Data is null)
            {
                return ApiResponse<AppointmentResponseDto>.Error(
                    slotsResponse.StatusCode,
                    slotsResponse.Message);
            }

            var selectedSlot = slotsResponse.Data.FirstOrDefault(slot =>
                slot.StartsAtUtc == startUtc);
            if (selectedSlot is null)
            {
                return ApiResponse<AppointmentResponseDto>.Error(
                    409,
                    "The selected appointment slot is no longer available.");
            }

            var appointment = new Appointment(
                clinic.Id,
                doctor.Id,
                currentUserId,
                selectedSlot.StartsAtUtc,
                selectedSlot.EndsAtUtc);

            _dbContext.Appointments.Add(appointment);
            await _dbContext.SaveChangesAsync();
            if (transaction is not null)
            {
                await transaction.CommitAsync();
            }

            var bookedAppointment = await LoadAppointmentAsync(appointment.Id);
            return ApiResponse<AppointmentResponseDto>.Success(
                ToDto(bookedAppointment!, clinicTimeZone),
                "Appointment booked.",
                201);
        }
        catch (DbUpdateException exception) when (IsBookingRace(exception))
        {
            return ApiResponse<AppointmentResponseDto>.Error(
                409,
                "The selected appointment slot has just been booked.");
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            return ApiResponse<AppointmentResponseDto>.Error(
                409,
                "The selected appointment slot conflicts with another booking.");
        }
    }

    public async Task<ApiResponse<List<AppointmentResponseDto>>> GetPatientAppointmentsAsync(
        Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            return ApiResponse<List<AppointmentResponseDto>>.Error(401, "User not authenticated.");
        }

        var appointments = await _dbContext.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Clinic)
            .Include(appointment => appointment.DoctorProfile)
                .ThenInclude(doctor => doctor.User)
            .Include(appointment => appointment.Patient)
            .Where(appointment => appointment.PatientUserId == currentUserId)
            .OrderBy(appointment => appointment.StartsAtUtc)
            .ToListAsync();

        var response = appointments.Select(appointment =>
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(appointment.Clinic.TimeZoneId);
            return ToDto(appointment, timeZone);
        }).ToList();

        return ApiResponse<List<AppointmentResponseDto>>.Success(response);
    }

    public async Task<ApiResponse<List<AppointmentResponseDto>>> GetClinicAppointmentsAsync(
        Guid currentUserId,
        Guid clinicId,
        DateOnly date)
    {
        var clinic = await _dbContext.Clinics.FirstOrDefaultAsync(item => item.Id == clinicId);
        if (clinic is null)
        {
            return ApiResponse<List<AppointmentResponseDto>>.Error(404, "Clinic not found.");
        }

        var membership = await ClinicAccess.GetMembershipAsync(_dbContext, currentUserId, clinicId);
        if (membership is null || membership.Role == ClinicRole.Patient)
        {
            return ApiResponse<List<AppointmentResponseDto>>.Error(
                403,
                "You cannot view clinic appointments.");
        }

        var query = _dbContext.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Clinic)
            .Include(appointment => appointment.DoctorProfile)
                .ThenInclude(doctor => doctor.User)
            .Include(appointment => appointment.Patient)
            .Where(appointment => appointment.ClinicId == clinicId);

        if (membership.Role == ClinicRole.Doctor)
        {
            query = query.Where(appointment =>
                appointment.DoctorProfile.UserId == currentUserId);
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(clinic.TimeZoneId);
        if (!TryGetUtcDateRange(date, timeZone, out var startsAtUtc, out var endsAtUtc))
        {
            return ApiResponse<List<AppointmentResponseDto>>.Error(
                400,
                "The clinic-local date cannot be represented unambiguously.");
        }

        query = query.Where(appointment =>
            appointment.StartsAtUtc >= startsAtUtc
            && appointment.StartsAtUtc < endsAtUtc);

        var appointments = await query
            .OrderBy(appointment => appointment.StartsAtUtc)
            .ToListAsync();

        return ApiResponse<List<AppointmentResponseDto>>.Success(
            appointments.Select(appointment => ToDto(appointment, timeZone)).ToList());
    }

    public async Task<ApiResponse<AppointmentResponseDto>> CancelAppointmentAsync(
        Guid currentUserId,
        Guid appointmentId)
    {
        var appointment = await LoadAppointmentAsync(appointmentId);
        if (appointment is null)
        {
            return ApiResponse<AppointmentResponseDto>.Error(404, "Appointment not found.");
        }

        if (appointment.PatientUserId != currentUserId
            && !await CanManageAppointmentAsync(currentUserId, appointment))
        {
            return ApiResponse<AppointmentResponseDto>.Error(403, "You cannot cancel this appointment.");
        }

        var now = DateTimeOffset.UtcNow;
        if (appointment.Status != AppointmentStatus.Booked || appointment.StartsAtUtc <= now)
        {
            return ApiResponse<AppointmentResponseDto>.Error(
                409,
                "Only future booked appointments can be cancelled.");
        }

        appointment.Cancel(now);
        await _dbContext.SaveChangesAsync();

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(appointment.Clinic.TimeZoneId);
        return ApiResponse<AppointmentResponseDto>.Success(ToDto(appointment, timeZone));
    }

    public async Task<ApiResponse<AppointmentResponseDto>> CompleteAppointmentAsync(
        Guid currentUserId,
        Guid appointmentId)
    {
        var appointment = await LoadAppointmentAsync(appointmentId);
        if (appointment is null)
        {
            return ApiResponse<AppointmentResponseDto>.Error(404, "Appointment not found.");
        }

        if (!await CanManageAppointmentAsync(currentUserId, appointment))
        {
            return ApiResponse<AppointmentResponseDto>.Error(403, "You cannot complete this appointment.");
        }

        var now = DateTimeOffset.UtcNow;
        if (appointment.Status != AppointmentStatus.Booked || appointment.EndsAtUtc > now)
        {
            return ApiResponse<AppointmentResponseDto>.Error(
                409,
                "Only ended booked appointments can be completed.");
        }

        appointment.Complete(now);
        await _dbContext.SaveChangesAsync();

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(appointment.Clinic.TimeZoneId);
        return ApiResponse<AppointmentResponseDto>.Success(ToDto(appointment, timeZone));
    }

    private Task<Appointment?> LoadAppointmentAsync(Guid appointmentId)
    {
        return _dbContext.Appointments
            .Include(appointment => appointment.Clinic)
            .Include(appointment => appointment.DoctorProfile)
                .ThenInclude(doctor => doctor.User)
            .Include(appointment => appointment.Patient)
            .FirstOrDefaultAsync(appointment => appointment.Id == appointmentId);
    }

    private async Task<bool> CanManageAppointmentAsync(
        Guid currentUserId,
        Appointment appointment)
    {
        var membership = await ClinicAccess.GetMembershipAsync(
            _dbContext,
            currentUserId,
            appointment.ClinicId);

        if (membership is null)
        {
            return false;
        }

        return membership.Role is ClinicRole.Receptionist
                or ClinicRole.ClinicAdministrator
                or ClinicRole.Owner
            || (membership.Role == ClinicRole.Doctor
                && appointment.DoctorProfile.UserId == currentUserId);
    }

    private static AppointmentResponseDto ToDto(
        Appointment appointment,
        TimeZoneInfo clinicTimeZone)
    {
        return new AppointmentResponseDto
        {
            Id = appointment.Id,
            ClinicId = appointment.ClinicId,
            ClinicName = appointment.Clinic.Name,
            DoctorProfileId = appointment.DoctorProfileId,
            DoctorUserId = appointment.DoctorProfile.UserId,
            DoctorName = $"{appointment.DoctorProfile.FirstName} {appointment.DoctorProfile.LastName}".Trim(),
            PatientUserId = appointment.PatientUserId,
            PatientName = appointment.Patient.DisplayName,
            PatientEmail = appointment.Patient.Email,
            StartsAtUtc = appointment.StartsAtUtc,
            EndsAtUtc = appointment.EndsAtUtc,
            LocalStart = TimeZoneInfo.ConvertTime(appointment.StartsAtUtc, clinicTimeZone).DateTime,
            LocalEnd = TimeZoneInfo.ConvertTime(appointment.EndsAtUtc, clinicTimeZone).DateTime,
            TimeZoneId = clinicTimeZone.Id,
            Status = appointment.Status,
            CreatedAtUtc = appointment.CreatedAtUtc
        };
    }

    private static bool TryGetUtcDateRange(
        DateOnly date,
        TimeZoneInfo timeZone,
        out DateTimeOffset startUtc,
        out DateTimeOffset endUtc)
    {
        var localStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var localEnd = date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        if (!TryConvertLocalToUtc(localStart, timeZone, out startUtc)
            || !TryConvertLocalToUtc(localEnd, timeZone, out endUtc))
        {
            startUtc = default;
            endUtc = default;
            return false;
        }

        return true;
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

    private static bool IsBookingRace(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException databaseException)
        {
            return false;
        }

        return databaseException.SqlState == PostgresErrorCodes.SerializationFailure
            || (databaseException.SqlState == PostgresErrorCodes.UniqueViolation
                && databaseException.ConstraintName == ActiveBookingIndexName);
    }
}
