using Domain.Entities.Clinic;
using Domain.Enums;

namespace UnitTests.Domain.Clinic;

public sealed class AppointmentTests
{
    [Fact]
    public void Constructor_normalizes_the_appointment_instants_to_utc()
    {
        var start = new DateTimeOffset(2030, 1, 7, 10, 0, 0, TimeSpan.FromHours(2));
        var end = start.AddMinutes(30);

        var appointment = CreateAppointment(startsAt: start, endsAt: end);

        Assert.NotEqual(Guid.Empty, appointment.Id);
        Assert.Equal(start.ToUniversalTime(), appointment.StartsAtUtc);
        Assert.Equal(end.ToUniversalTime(), appointment.EndsAtUtc);
        Assert.Equal(TimeSpan.Zero, appointment.StartsAtUtc.Offset);
        Assert.Equal(AppointmentStatus.Booked, appointment.Status);
        Assert.Equal(appointment.CreatedAtUtc, appointment.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_rejects_an_empty_clinic_id()
    {
        Assert.Throws<ArgumentException>(() => CreateAppointment(clinicId: Guid.Empty));
    }

    [Fact]
    public void Constructor_rejects_an_empty_doctor_profile_id()
    {
        Assert.Throws<ArgumentException>(() => CreateAppointment(doctorProfileId: Guid.Empty));
    }

    [Fact]
    public void Constructor_rejects_an_empty_patient_id()
    {
        Assert.Throws<ArgumentException>(() => CreateAppointment(patientUserId: Guid.Empty));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void Constructor_rejects_an_end_that_is_not_after_the_start(int minutesFromStart)
    {
        var startsAt = DateTimeOffset.UtcNow.AddDays(1);

        Assert.Throws<ArgumentException>(() => CreateAppointment(
            startsAt: startsAt,
            endsAt: startsAt.AddMinutes(minutesFromStart)));
    }

    [Fact]
    public void Cancel_changes_a_future_booking_to_cancelled_and_stores_utc_update_time()
    {
        var now = DateTimeOffset.UtcNow;
        var appointment = CreateAppointment(startsAt: now.AddHours(1), endsAt: now.AddHours(2));

        appointment.Cancel(now.ToOffset(TimeSpan.FromHours(3)));

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Equal(now, appointment.UpdatedAtUtc);
        Assert.Equal(TimeSpan.Zero, appointment.UpdatedAtUtc.Offset);
    }

    [Fact]
    public void Cancel_rejects_an_appointment_that_has_started()
    {
        var now = DateTimeOffset.UtcNow;
        var appointment = CreateAppointment(startsAt: now, endsAt: now.AddMinutes(30));

        Assert.Throws<InvalidOperationException>(() => appointment.Cancel(now));
    }

    [Fact]
    public void Cancel_rejects_a_booking_that_is_already_cancelled()
    {
        var now = DateTimeOffset.UtcNow;
        var appointment = CreateAppointment(startsAt: now.AddHours(1), endsAt: now.AddHours(2));
        appointment.Cancel(now);

        Assert.Throws<InvalidOperationException>(() => appointment.Cancel(now));
    }

    [Fact]
    public void Cancel_rejects_a_booking_that_is_already_completed()
    {
        var now = DateTimeOffset.UtcNow;
        var appointment = CreateAppointment(startsAt: now.AddHours(-2), endsAt: now.AddHours(-1));
        appointment.Complete(now);

        Assert.Throws<InvalidOperationException>(() => appointment.Cancel(now));
    }

    [Fact]
    public void Complete_changes_an_ended_booking_to_completed()
    {
        var now = DateTimeOffset.UtcNow;
        var appointment = CreateAppointment(startsAt: now.AddHours(-2), endsAt: now.AddHours(-1));

        appointment.Complete(now.ToOffset(TimeSpan.FromHours(-4)));

        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
        Assert.Equal(now, appointment.UpdatedAtUtc);
        Assert.Equal(TimeSpan.Zero, appointment.UpdatedAtUtc.Offset);
    }

    [Fact]
    public void Complete_rejects_a_booking_that_has_not_ended()
    {
        var now = DateTimeOffset.UtcNow;
        var appointment = CreateAppointment(startsAt: now.AddMinutes(-15), endsAt: now.AddMinutes(15));

        Assert.Throws<InvalidOperationException>(() => appointment.Complete(now));
    }

    [Fact]
    public void Complete_rejects_a_booking_that_is_already_cancelled()
    {
        var now = DateTimeOffset.UtcNow;
        var appointment = CreateAppointment(startsAt: now.AddHours(1), endsAt: now.AddHours(2));
        appointment.Cancel(now);

        Assert.Throws<InvalidOperationException>(() => appointment.Complete(now.AddDays(1)));
    }

    [Fact]
    public void Complete_rejects_a_booking_that_is_already_completed()
    {
        var now = DateTimeOffset.UtcNow;
        var appointment = CreateAppointment(startsAt: now.AddHours(-2), endsAt: now.AddHours(-1));
        appointment.Complete(now);

        Assert.Throws<InvalidOperationException>(() => appointment.Complete(now.AddMinutes(1)));
    }

    private static Appointment CreateAppointment(
        Guid? clinicId = null,
        Guid? doctorProfileId = null,
        Guid? patientUserId = null,
        DateTimeOffset? startsAt = null,
        DateTimeOffset? endsAt = null)
    {
        var start = startsAt ?? DateTimeOffset.UtcNow.AddDays(1);
        return new Appointment(
            clinicId ?? Guid.NewGuid(),
            doctorProfileId ?? Guid.NewGuid(),
            patientUserId ?? Guid.NewGuid(),
            start,
            endsAt ?? start.AddMinutes(30));
    }
}
