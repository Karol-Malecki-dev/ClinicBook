using Domain.Enums;

namespace Domain.Entities.Clinic;

public sealed class ClinicMembership
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ClinicId { get; private set; }
    public ClinicRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public User User { get; private set; } = null!;
    public Clinic Clinic { get; private set; } = null!;

    private ClinicMembership()
    {
        // Required by EF Core.
    }

    public ClinicMembership(Guid userId, Guid clinicId, ClinicRole role)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (clinicId == Guid.Empty)
        {
            throw new ArgumentException("Clinic ID cannot be empty.", nameof(clinicId));
        }

        ValidateRole(role);

        Id = Guid.NewGuid();
        UserId = userId;
        ClinicId = clinicId;
        Role = role;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void ChangeRole(ClinicRole role)
    {
        ValidateRole(role);
        Role = role;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static void ValidateRole(ClinicRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentException("Invalid clinic role.", nameof(role));
        }
    }
}
