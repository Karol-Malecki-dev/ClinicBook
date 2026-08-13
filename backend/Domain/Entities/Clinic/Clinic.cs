using Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Domain.Entities.Clinic
{
    public class Clinic
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public Address Address { get; private set; } = null!;
        public string? PhoneNumber { get; private set; }
        public string? PhoneNumber2 { get; private set; }
        public string? PhoneNumber3 { get; private set; }
        public string? Email { get; private set; }
        public string? Website { get; private set; }
        public TimeOnly OpenTime { get; private set; }
        public TimeOnly CloseTime { get; private set; }

        public ICollection<ClinicMembership> Memberships { get; private set; } = new List<ClinicMembership>();
        public ICollection<ClinicRoom> Rooms { get; private set; } = new List<ClinicRoom>();

        private Clinic() { } // Private constructor for EF Core

        public Clinic(string name, Address address, TimeOnly openTime, TimeOnly closeTime)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Clinic name cannot be null or empty.", nameof(name));
            }
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address), "Address cannot be null.");
            }
            Id = Guid.NewGuid();
            Name = name;
            Address = address;
            OpenTime = openTime;
            CloseTime = closeTime;
        }

        public void UpdateDetails(
            string name,
            Address address,
            TimeOnly openTime,
            TimeOnly closeTime,
            string? phoneNumber = null,
            string? phoneNumber2 = null,
            string? phoneNumber3 = null,
            string? email = null,
            string? website = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Clinic name cannot be null or empty.", nameof(name));
            }

            ArgumentNullException.ThrowIfNull(address);

            Name = name.Trim();
            Address = address;
            OpenTime = openTime;
            CloseTime = closeTime;
            PhoneNumber = phoneNumber?.Trim();
            PhoneNumber2 = phoneNumber2?.Trim();
            PhoneNumber3 = phoneNumber3?.Trim();
            Email = email?.Trim();
            Website = website?.Trim();
        }



    }
}
