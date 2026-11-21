using API.Validators.Clinic;
using API.Validators.Clinic.ClinicMembership;
using Application.DTOs.Clinic;
using Application.DTOs.ClinicMembership;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace UnitTests.Validators;

public sealed class ClinicBookingRequestValidatorTests
{
    private readonly SetDoctorWeeklyAvailabilityRequestDtoValidator _availabilityValidator = new();
    private readonly CreateAppointmentRequestDtoValidator _appointmentValidator = new();
    private readonly AddClinicMembershipRequestDtoValidator _membershipValidator = new();

    [Fact]
    public void Availability_validator_accepts_a_valid_window()
    {
        var result = _availabilityValidator.TestValidate(new SetDoctorWeeklyAvailabilityRequestDto
        {
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            SlotDurationMinutes = 30
        });

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Availability_validator_rejects_an_undefined_weekday()
    {
        var result = _availabilityValidator.TestValidate(new SetDoctorWeeklyAvailabilityRequestDto
        {
            DayOfWeek = (DayOfWeek)7,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            SlotDurationMinutes = 30
        });

        result.ShouldHaveValidationErrorFor(request => request.DayOfWeek);
    }

    [Theory]
    [InlineData(9, 0, 9, 0)]
    [InlineData(10, 0, 9, 0)]
    public void Availability_validator_rejects_a_non_positive_window(
        int startHour,
        int startMinute,
        int endHour,
        int endMinute)
    {
        var result = _availabilityValidator.TestValidate(new SetDoctorWeeklyAvailabilityRequestDto
        {
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(startHour, startMinute),
            EndTime = new TimeOnly(endHour, endMinute),
            SlotDurationMinutes = 30
        });

        result.ShouldHaveValidationErrorFor(request => request.EndTime);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    [InlineData(121)]
    public void Availability_validator_rejects_slot_durations_outside_the_supported_range(
        int slotDurationMinutes)
    {
        var result = _availabilityValidator.TestValidate(CreateAvailabilityRequest(
            slotDurationMinutes: slotDurationMinutes));

        result.ShouldHaveValidationErrorFor(request => request.SlotDurationMinutes);
    }

    [Fact]
    public void Availability_validator_rejects_a_duration_not_in_fifteen_minute_increments()
    {
        var result = _availabilityValidator.TestValidate(CreateAvailabilityRequest(
            slotDurationMinutes: 22));

        result.ShouldHaveValidationErrorFor(request => request.SlotDurationMinutes);
    }

    [Fact]
    public void Availability_validator_rejects_a_window_not_divisible_by_the_slot_duration()
    {
        var result = _availabilityValidator.TestValidate(CreateAvailabilityRequest(
            endTime: new TimeOnly(10, 15)));

        Assert.Contains(result.Errors, error =>
            error.ErrorMessage == "Availability window length must be divisible by the slot duration.");
    }

    [Fact]
    public void Appointment_validator_accepts_a_non_default_offset_instant()
    {
        var result = _appointmentValidator.TestValidate(new CreateAppointmentRequestDto
        {
            StartsAtUtc = new DateTimeOffset(2030, 1, 7, 9, 0, 0, TimeSpan.FromHours(1))
        });

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Appointment_validator_rejects_a_default_start_instant()
    {
        var result = _appointmentValidator.TestValidate(new CreateAppointmentRequestDto());

        result.ShouldHaveValidationErrorFor(request => request.StartsAtUtc);
    }

    [Fact]
    public void Membership_validator_accepts_a_user_id_without_email()
    {
        var result = _membershipValidator.TestValidate(new AddClinicMembershipRequestDto
        {
            UserId = Guid.NewGuid(),
            Role = ClinicRole.Doctor
        });

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Membership_validator_accepts_an_email_without_user_id()
    {
        var result = _membershipValidator.TestValidate(new AddClinicMembershipRequestDto
        {
            Email = "doctor@example.com",
            Role = ClinicRole.Doctor
        });

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Membership_validator_rejects_missing_user_identification()
    {
        var result = _membershipValidator.TestValidate(new AddClinicMembershipRequestDto
        {
            Role = ClinicRole.Doctor
        });

        Assert.Contains(result.Errors, error =>
            error.ErrorMessage == "Provide either a user ID or an email address, but not both.");
    }

    [Fact]
    public void Membership_validator_rejects_both_user_id_and_email()
    {
        var result = _membershipValidator.TestValidate(new AddClinicMembershipRequestDto
        {
            UserId = Guid.NewGuid(),
            Email = "doctor@example.com",
            Role = ClinicRole.Doctor
        });

        Assert.Contains(result.Errors, error =>
            error.ErrorMessage == "Provide either a user ID or an email address, but not both.");
    }

    [Fact]
    public void Membership_validator_rejects_an_invalid_email()
    {
        var result = _membershipValidator.TestValidate(new AddClinicMembershipRequestDto
        {
            Email = "not-an-email",
            Role = ClinicRole.Doctor
        });

        result.ShouldHaveValidationErrorFor(request => request.Email);
    }

    [Fact]
    public void Membership_validator_rejects_an_undefined_role()
    {
        var result = _membershipValidator.TestValidate(new AddClinicMembershipRequestDto
        {
            Email = "doctor@example.com",
            Role = (ClinicRole)99
        });

        result.ShouldHaveValidationErrorFor(request => request.Role);
    }

    private static SetDoctorWeeklyAvailabilityRequestDto CreateAvailabilityRequest(
        int slotDurationMinutes = 30,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null)
    {
        return new SetDoctorWeeklyAvailabilityRequestDto
        {
            DayOfWeek = DayOfWeek.Monday,
            StartTime = startTime ?? new TimeOnly(9, 0),
            EndTime = endTime ?? new TimeOnly(10, 0),
            SlotDurationMinutes = slotDurationMinutes
        };
    }
}
