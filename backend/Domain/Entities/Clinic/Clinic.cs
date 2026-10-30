using Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Domain.Entities.Clinic
{
    public class Clinic
    {
        public const string DefaultTimeZoneId = "Europe/Warsaw";

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
        public string TimeZoneId { get; private set; } = DefaultTimeZoneId;

        public ICollection<ClinicMembership> Memberships { get; private set; } = new List<ClinicMembership>();
        public ICollection<ClinicRoom> Rooms { get; private set; } = new List<ClinicRoom>();
        public ICollection<DoctorWeeklyAvailability> DoctorWeeklyAvailabilities { get; private set; } = new List<DoctorWeeklyAvailability>();
        public ICollection<Appointment> Appointments { get; private set; } = new List<Appointment>();

        private Clinic() { } // Private constructor for EF Core

        public Clinic(
            string name,
            Address address,
            TimeOnly openTime,
            TimeOnly closeTime,
            string timeZoneId = DefaultTimeZoneId)
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
            TimeZoneId = NormalizeTimeZoneId(timeZoneId);
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
            string? website = null,
            string? timeZoneId = null)
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
            if (timeZoneId is not null)
            {
                TimeZoneId = NormalizeTimeZoneId(timeZoneId);
            }
        }

        private static string NormalizeTimeZoneId(string timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
            {
                throw new ArgumentException("Clinic time zone is required.", nameof(timeZoneId));
            }

            var normalizedTimeZoneId = timeZoneId.Trim();
            try
            {
                _ = TimeZoneInfo.FindSystemTimeZoneById(normalizedTimeZoneId);
            }
            catch (TimeZoneNotFoundException exception)
            {
                throw new ArgumentException(
                    "Clinic time zone is not supported.",
                    nameof(timeZoneId),
                    exception);
            }
            catch (InvalidTimeZoneException exception)
            {
                throw new ArgumentException(
                    "Clinic time zone is invalid.",
                    nameof(timeZoneId),
                    exception);
            }

            return normalizedTimeZoneId;
        }
    }
}
