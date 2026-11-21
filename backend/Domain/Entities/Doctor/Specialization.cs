namespace Domain.Entities.Doctor;

public sealed class Specialization
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// General description of the specialization shared by all doctors.
    /// </summary>
    public string? Description { get; private set; }

    public ICollection<DoctorSpecialization> DoctorSpecializations { get; private set; } = [];

    private Specialization()
    {
        // Required by EF Core.
    }

    public Specialization(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Specialization name cannot be empty.",
                nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim();
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException(
                "Specialization name cannot be empty.",
                nameof(newName));
        }

        Name = newName.Trim();
    }

    public void UpdateDescription(string? newDescription)
    {
        Description = newDescription?.Trim();
    }
}
