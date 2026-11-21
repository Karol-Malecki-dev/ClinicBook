namespace Domain.Entities.Clinic;

public sealed class ClinicRoom
{
    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public Clinic Clinic { get; private set; } = null!;

    private ClinicRoom()
    {
        // Required by EF Core.
    }

    public ClinicRoom(Guid clinicId, string name, string? description = null)
    {
        if (clinicId == Guid.Empty)
        {
            throw new ArgumentException("Clinic ID cannot be empty.", nameof(clinicId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Room name cannot be empty.", nameof(name));
        }

        Id = Guid.NewGuid();
        ClinicId = clinicId;
        Name = name.Trim();
        Description = description?.Trim();
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("Room name cannot be empty.", nameof(newName));
        }

        Name = newName.Trim();
    }

    public void UpdateDescription(string? newDescription)
    {
        Description = newDescription?.Trim();
    }
}
