using Domain.Entities.Clinic;

namespace UnitTests.Domain.Clinic;

public sealed class DoctorWeeklyAvailabilityTests
{
    [Fact]
    public void Constructor_stores_a_valid_weekly_window()
    {
        var clinicId = Guid.NewGuid();
        var doctorProfileId = Guid.NewGuid();

        var availability = new DoctorWeeklyAvailability(
            clinicId,
            doctorProfileId,
            DayOfWeek.Monday,
            new TimeOnly(9, 0),
            new TimeOnly(10, 0),
            30);

        Assert.NotEqual(Guid.Empty, availability.Id);
        Assert.Equal(clinicId, availability.ClinicId);
        Assert.Equal(doctorProfileId, availability.DoctorProfileId);
        Assert.Equal(DayOfWeek.Monday, availability.DayOfWeek);
        Assert.Equal(new TimeOnly(9, 0), availability.StartTime);
        Assert.Equal(new TimeOnly(10, 0), availability.EndTime);
        Assert.Equal(30, availability.SlotDurationMinutes);
    }

    [Fact]
    public void Constructor_rejects_an_empty_clinic_id()
    {
        Assert.Throws<ArgumentException>(() => CreateAvailability(clinicId: Guid.Empty));
    }

    [Fact]
    public void Constructor_rejects_an_empty_doctor_profile_id()
    {
        Assert.Throws<ArgumentException>(() => CreateAvailability(doctorProfileId: Guid.Empty));
    }

    [Fact]
    public void Constructor_rejects_an_undefined_weekday()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateAvailability(dayOfWeek: (DayOfWeek)7));
    }

    [Theory]
    [InlineData(9, 0, 9, 0)]
    [InlineData(10, 0, 9, 0)]
    public void Constructor_rejects_a_window_that_does_not_end_after_it_starts(
        int startHour,
        int startMinute,
        int endHour,
        int endMinute)
    {
        Assert.Throws<ArgumentException>(() => CreateAvailability(
            startTime: new TimeOnly(startHour, startMinute),
            endTime: new TimeOnly(endHour, endMinute)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    [InlineData(22)]
    [InlineData(121)]
    public void Constructor_rejects_an_invalid_slot_duration(int durationMinutes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateAvailability(slotDurationMinutes: durationMinutes));
    }

    [Fact]
    public void Constructor_rejects_a_window_not_divisible_by_slot_duration()
    {
        Assert.Throws<ArgumentException>(() => CreateAvailability(
            endTime: new TimeOnly(10, 15),
            slotDurationMinutes: 30));
    }

    [Fact]
    public void Update_replaces_the_schedule_when_the_new_values_are_valid()
    {
        var availability = CreateAvailability();

        availability.Update(
            DayOfWeek.Tuesday,
            new TimeOnly(10, 0),
            new TimeOnly(11, 30),
            30);

        Assert.Equal(DayOfWeek.Tuesday, availability.DayOfWeek);
        Assert.Equal(new TimeOnly(10, 0), availability.StartTime);
        Assert.Equal(new TimeOnly(11, 30), availability.EndTime);
        Assert.Equal(30, availability.SlotDurationMinutes);
    }

    [Fact]
    public void Update_does_not_change_the_schedule_when_validation_fails()
    {
        var availability = CreateAvailability();

        Assert.Throws<ArgumentException>(() => availability.Update(
            DayOfWeek.Tuesday,
            new TimeOnly(10, 0),
            new TimeOnly(11, 0),
            45));

        Assert.Equal(DayOfWeek.Monday, availability.DayOfWeek);
        Assert.Equal(new TimeOnly(9, 0), availability.StartTime);
        Assert.Equal(new TimeOnly(10, 0), availability.EndTime);
        Assert.Equal(30, availability.SlotDurationMinutes);
    }

    private static DoctorWeeklyAvailability CreateAvailability(
        Guid? clinicId = null,
        Guid? doctorProfileId = null,
        DayOfWeek dayOfWeek = DayOfWeek.Monday,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        int slotDurationMinutes = 30)
    {
        return new DoctorWeeklyAvailability(
            clinicId ?? Guid.NewGuid(),
            doctorProfileId ?? Guid.NewGuid(),
            dayOfWeek,
            startTime ?? new TimeOnly(9, 0),
            endTime ?? new TimeOnly(10, 0),
            slotDurationMinutes);
    }
}
