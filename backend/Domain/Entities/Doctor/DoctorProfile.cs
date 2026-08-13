using System;
using System.Collections.Generic;

namespace Domain.Entities.Doctor
{
    public class DoctorProfile
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public User User { get; private set; } = null!;
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public string Bio { get; private set; } = string.Empty;
        public string PhoneNumber { get; private set; } = string.Empty;
        public bool IsActive { get; private set; }
        public ICollection<DoctorSpecialization> DoctorSpecializations
        { get; private set; } = new List<DoctorSpecialization>();
    
        private DoctorProfile() { } // Private constructor for EF Core


        public DoctorProfile(Guid userId, string firstName, string lastName, string? bio = null, string? phoneNumber = null)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            }
            if (string.IsNullOrWhiteSpace(firstName))
            {
                throw new ArgumentException("First name cannot be null or empty.", nameof(firstName));
            }
            if (string.IsNullOrWhiteSpace(lastName))
            {
                throw new ArgumentException("Last name cannot be null or empty.", nameof(lastName));
            }
            if (phoneNumber is not null && string.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new ArgumentException("Phone number cannot be whitespace.", nameof(phoneNumber));
            }
            Id = Guid.NewGuid();
            UserId = userId;
            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            Bio = bio?.Trim() ?? string.Empty;
            PhoneNumber = phoneNumber?.Trim() ?? string.Empty;
            IsActive = true; // New doctors are active by default
        }

        public void UpdateDetails(
            string firstName,
            string lastName,
            string? bio = null,
            string? phoneNumber = null)
        {
            if (string.IsNullOrWhiteSpace(firstName))
            {
                throw new ArgumentException("First name cannot be null or empty.", nameof(firstName));
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                throw new ArgumentException("Last name cannot be null or empty.", nameof(lastName));
            }

            if (phoneNumber is not null && string.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new ArgumentException("Phone number cannot be whitespace.", nameof(phoneNumber));
            }

            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            Bio = bio?.Trim() ?? string.Empty;
            PhoneNumber = phoneNumber?.Trim() ?? string.Empty;
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

}
