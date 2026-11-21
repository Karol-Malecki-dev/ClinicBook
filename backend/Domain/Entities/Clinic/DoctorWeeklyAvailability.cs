namespace Domain.Entities.Clinic;

/// <summary>
/// Defines a recurring clinic-local appointment window for one doctor.
/// </summary>
public sealed class DoctorWeeklyAvailability
{
    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid DoctorProfileId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int SlotDurationMinutes { get; private set; }

    public Clinic Clinic { get; private set; } = null!;
    public Domain.Entities.Doctor.DoctorProfile DoctorProfile { get; private set; } = null!;

    private DoctorWeeklyAvailability()
    {
    }

    public DoctorWeeklyAvailability(
        Guid clinicId,
        Guid doctorProfileId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes)
    {
        if (clinicId == Guid.Empty)
        {
            throw new ArgumentException("Clinic ID cannot be empty.", nameof(clinicId));
        }

        if (doctorProfileId == Guid.Empty)
        {
            throw new ArgumentException("Doctor profile ID cannot be empty.", nameof(doctorProfileId));
        }

        ValidateSchedule(dayOfWeek, startTime, endTime, slotDurationMinutes);

        Id = Guid.NewGuid();
        ClinicId = clinicId;
        DoctorProfileId = doctorProfileId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        SlotDurationMinutes = slotDurationMinutes;
    }

    public void Update(
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes)
    {
        ValidateSchedule(dayOfWeek, startTime, endTime, slotDurationMinutes);

        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        SlotDurationMinutes = slotDurationMinutes;
    }

    private static void ValidateSchedule(
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes)
    {
        if (!Enum.IsDefined(dayOfWeek))
        {
            throw new ArgumentOutOfRangeException(nameof(dayOfWeek));
        }

        if (endTime <= startTime)
        {
            throw new ArgumentException("Availability must end after it starts.");
        }

        if (slotDurationMinutes is < 15 or > 120 || slotDurationMinutes % 15 != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotDurationMinutes),
                "Slot duration must be from 15 to 120 minutes in 15-minute increments.");
        }

        var duration = endTime - startTime;
        if (duration.Ticks % TimeSpan.FromMinutes(slotDurationMinutes).Ticks != 0)
        {
            throw new ArgumentException(
                "Availability window length must be divisible by the slot duration.");
        }
    }
}
