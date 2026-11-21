using Domain.Entities.Doctor;
using Domain.Enums;

namespace Domain.Entities.Clinic;

/// <summary>
/// Defines a one-off availability addition or closure in clinic-local time.
/// </summary>
public sealed class DoctorScheduleException
{
    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid DoctorProfileId { get; private set; }
    public DateOnly Date { get; private set; }
    public ScheduleExceptionType Type { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int SlotDurationMinutes { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Clinic Clinic { get; private set; } = null!;
    public DoctorProfile DoctorProfile { get; private set; } = null!;

    private DoctorScheduleException()
    {
    }

    public DoctorScheduleException(
        Guid clinicId,
        Guid doctorProfileId,
        DateOnly date,
        ScheduleExceptionType type,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes,
        string? reason = null)
    {
        if (clinicId == Guid.Empty)
        {
            throw new ArgumentException("Clinic ID cannot be empty.", nameof(clinicId));
        }

        if (doctorProfileId == Guid.Empty)
        {
            throw new ArgumentException("Doctor profile ID cannot be empty.", nameof(doctorProfileId));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        if (endTime <= startTime)
        {
            throw new ArgumentException("Schedule exception must end after it starts.");
        }

        if (slotDurationMinutes is < 15 or > 120 || slotDurationMinutes % 15 != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotDurationMinutes),
                "Slot duration must be from 15 to 120 minutes in 15-minute increments.");
        }

        if (type == ScheduleExceptionType.Available
            && (endTime - startTime).Ticks % TimeSpan.FromMinutes(slotDurationMinutes).Ticks != 0)
        {
            throw new ArgumentException(
                "Additional availability length must be divisible by the slot duration.");
        }

        var normalizedReason = reason?.Trim();
        if (normalizedReason is { Length: > 300 })
        {
            throw new ArgumentException("Reason cannot exceed 300 characters.", nameof(reason));
        }

        Id = Guid.NewGuid();
        ClinicId = clinicId;
        DoctorProfileId = doctorProfileId;
        Date = date;
        Type = type;
        StartTime = startTime;
        EndTime = endTime;
        SlotDurationMinutes = slotDurationMinutes;
        Reason = string.IsNullOrWhiteSpace(normalizedReason) ? null : normalizedReason;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }
}
