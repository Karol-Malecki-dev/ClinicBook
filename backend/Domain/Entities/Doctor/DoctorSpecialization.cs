namespace Domain.Entities.Doctor;

public sealed class DoctorSpecialization
{
    public Guid Id { get; private set; }
    public Guid DoctorProfileId { get; private set; }
    public Guid SpecializationId { get; private set; }

    /// <summary>
    /// Doctor-specific experience or context related to this specialization.
    /// </summary>
    public string? ExperienceDescription { get; private set; }

    public DoctorProfile DoctorProfile { get; private set; } = null!;
    public Specialization Specialization { get; private set; } = null!;

    private DoctorSpecialization()
    {
        // Required by EF Core.
    }

    public DoctorSpecialization(
        DoctorProfile doctorProfile,
        Specialization specialization,
        string? experienceDescription = null)
    {
        ArgumentNullException.ThrowIfNull(doctorProfile);
        ArgumentNullException.ThrowIfNull(specialization);

        if (doctorProfile.Id == Guid.Empty)
        {
            throw new ArgumentException(
                "Doctor profile ID cannot be empty.",
                nameof(doctorProfile));
        }

        if (specialization.Id == Guid.Empty)
        {
            throw new ArgumentException(
                "Specialization ID cannot be empty.",
                nameof(specialization));
        }

        Id = Guid.NewGuid();
        DoctorProfile = doctorProfile;
        DoctorProfileId = doctorProfile.Id;
        Specialization = specialization;
        SpecializationId = specialization.Id;
        ExperienceDescription = experienceDescription?.Trim();
    }

    public void UpdateExperienceDescription(string? experienceDescription)
    {
        ExperienceDescription = experienceDescription?.Trim();
    }
}
