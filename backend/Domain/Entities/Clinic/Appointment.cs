using Domain.Enums;

namespace Domain.Entities.Clinic;

/// <summary>
/// Represents a patient booking. Start and end are stored as UTC instants.
/// </summary>
public sealed class Appointment
{
    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid DoctorProfileId { get; private set; }
    public Guid PatientUserId { get; private set; }
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset EndsAtUtc { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Clinic Clinic { get; private set; } = null!;
    public Domain.Entities.Doctor.DoctorProfile DoctorProfile { get; private set; } = null!;
    public Domain.Entities.User Patient { get; private set; } = null!;

    private Appointment()
    {
    }

    public Appointment(
        Guid clinicId,
        Guid doctorProfileId,
        Guid patientUserId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt)
    {
        if (clinicId == Guid.Empty)
        {
            throw new ArgumentException("Clinic ID cannot be empty.", nameof(clinicId));
        }

        if (doctorProfileId == Guid.Empty)
        {
            throw new ArgumentException("Doctor profile ID cannot be empty.", nameof(doctorProfileId));
        }

        if (patientUserId == Guid.Empty)
        {
            throw new ArgumentException("Patient user ID cannot be empty.", nameof(patientUserId));
        }

        if (endsAt <= startsAt)
        {
            throw new ArgumentException("Appointment must end after it starts.");
        }

        Id = Guid.NewGuid();
        ClinicId = clinicId;
        DoctorProfileId = doctorProfileId;
        PatientUserId = patientUserId;
        StartsAtUtc = startsAt.ToUniversalTime();
        EndsAtUtc = endsAt.ToUniversalTime();
        Status = AppointmentStatus.Booked;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status != AppointmentStatus.Booked)
        {
            throw new InvalidOperationException("Only booked appointments can be cancelled.");
        }

        if (StartsAtUtc <= now.ToUniversalTime())
        {
            throw new InvalidOperationException("An appointment cannot be cancelled after it has started.");
        }

        Status = AppointmentStatus.Cancelled;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Complete(DateTimeOffset now)
    {
        if (Status != AppointmentStatus.Booked)
        {
            throw new InvalidOperationException("Only booked appointments can be completed.");
        }

        if (EndsAtUtc > now.ToUniversalTime())
        {
            throw new InvalidOperationException("An appointment cannot be completed before it ends.");
        }

        Status = AppointmentStatus.Completed;
        UpdatedAtUtc = now.ToUniversalTime();
    }
}
