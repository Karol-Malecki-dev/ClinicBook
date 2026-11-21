using Domain.ValueObjects;

namespace Domain.Entities.Patient;

public sealed class PatientProfile
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public Address? Address { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public bool IsActive { get; private set; }
    public User User { get; private set; } = null!;

    private PatientProfile()
    {
        // Required by EF Core.
    }

    public PatientProfile(
        Guid userId,
        string name,
        DateOnly dateOfBirth,
        string? phoneNumber = null,
        Address? address = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Patient name cannot be empty.", nameof(name));
        }

        if (dateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentException(
                "Date of birth cannot be in the future.",
                nameof(dateOfBirth));
        }

        if (phoneNumber is not null && string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException(
                "Phone number cannot be whitespace.",
                nameof(phoneNumber));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        Name = name.Trim();
        DateOfBirth = dateOfBirth;
        PhoneNumber = phoneNumber?.Trim();
        Address = address;
        IsActive = true;
    }

        public void UpdateDetails(
            string name,
            DateOnly dateOfBirth,
            string? phoneNumber = null,
            Address? address = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Patient name cannot be empty.", nameof(name));
            }

            if (dateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
            {
                throw new ArgumentException(
                    "Date of birth cannot be in the future.",
                    nameof(dateOfBirth));
            }

            if (phoneNumber is not null && string.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new ArgumentException(
                    "Phone number cannot be whitespace.",
                    nameof(phoneNumber));
            }

            Name = name.Trim();
            DateOfBirth = dateOfBirth;
            PhoneNumber = phoneNumber?.Trim();
            Address = address;
        }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
