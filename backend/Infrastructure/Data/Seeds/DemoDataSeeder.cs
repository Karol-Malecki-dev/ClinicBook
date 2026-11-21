using Domain.Entities;
using Domain.Entities.Clinic;
using Domain.Entities.Doctor;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Settings;

namespace Infrastructure.Data.Seeds;

/// <summary>
/// Seeds deterministic accounts, clinic membership, and doctor availability
/// for an explicitly enabled local demo environment.
/// </summary>
public sealed class DemoDataSeeder
{
    private const string ClinicName = "ClinicBook Demo Clinic";

    private readonly DemoDataSettings _settings;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public DemoDataSeeder(IOptions<DemoDataSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SeedAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        var owner = await GetOrCreateUserAsync(
            dbContext,
            _settings.OwnerEmail,
            "Demo Owner",
            cancellationToken);
        var doctor = await GetOrCreateUserAsync(
            dbContext,
            _settings.DoctorEmail,
            "Anna Nowak",
            cancellationToken);
        var patient = await GetOrCreateUserAsync(
            dbContext,
            _settings.PatientEmail,
            "Demo Patient",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var clinic = await dbContext.Clinics
            .FirstOrDefaultAsync(item => item.Name == ClinicName, cancellationToken);
        if (clinic is null)
        {
            clinic = new Clinic(
                ClinicName,
                new Address
                {
                    Street = "Main Street",
                    BuildingNumber = "1",
                    City = "Warsaw",
                    PostalCode = "00-001",
                    Country = "Poland"
                },
                new TimeOnly(8, 0),
                new TimeOnly(18, 0),
                "Europe/Warsaw");
            clinic.UpdateDetails(
                ClinicName,
                clinic.Address,
                clinic.OpenTime,
                clinic.CloseTime,
                "+48 22 000 00 00",
                email: "demo@clinicbook.local");

            dbContext.Clinics.Add(clinic);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await EnsureMembershipAsync(dbContext, owner.Id, clinic.Id, ClinicRole.Owner, cancellationToken);
        await EnsureMembershipAsync(dbContext, doctor.Id, clinic.Id, ClinicRole.Doctor, cancellationToken);
        await EnsureMembershipAsync(dbContext, patient.Id, clinic.Id, ClinicRole.Patient, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var doctorProfile = await dbContext.DoctorProfiles
            .FirstOrDefaultAsync(item => item.UserId == doctor.Id, cancellationToken);
        if (doctorProfile is null)
        {
            doctorProfile = new DoctorProfile(
                doctor.Id,
                "Anna",
                "Nowak",
                "Demo physician profile for the ClinicBook booking flow.",
                "+48 500 000 001");
            dbContext.DoctorProfiles.Add(doctorProfile);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var day in new[]
                 {
                     DayOfWeek.Monday,
                     DayOfWeek.Tuesday,
                     DayOfWeek.Wednesday,
                     DayOfWeek.Thursday,
                     DayOfWeek.Friday
                 })
        {
            var exists = await dbContext.DoctorWeeklyAvailabilities.AnyAsync(
                item => item.ClinicId == clinic.Id
                    && item.DoctorProfileId == doctorProfile.Id
                    && item.DayOfWeek == day
                    && item.StartTime == new TimeOnly(9, 0),
                cancellationToken);
            if (!exists)
            {
                dbContext.DoctorWeeklyAvailabilities.Add(new DoctorWeeklyAvailability(
                    clinic.Id,
                    doctorProfile.Id,
                    day,
                    new TimeOnly(9, 0),
                    new TimeOnly(12, 0),
                    30));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetOrCreateUserAsync(
        ApplicationDbContext dbContext,
        string configuredEmail,
        string displayName,
        CancellationToken cancellationToken)
    {
        var email = configuredEmail.Trim().ToLowerInvariant();
        var existing = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
        if (existing is not null)
        {
            existing.DisplayName = displayName;
            existing.Role = UserRole.User;
            existing.IsActive = true;
            existing.IsEmailConfirmed = true;
            existing.IsTwoFactorEnabled = false;
            existing.PasswordHash = _passwordHasher.HashPassword(existing, _settings.Password);
            return existing;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            Role = UserRole.User,
            IsActive = true,
            IsEmailConfirmed = true,
            IsTwoFactorEnabled = false,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, _settings.Password);
        dbContext.Users.Add(user);
        return user;
    }

    private static async Task EnsureMembershipAsync(
        ApplicationDbContext dbContext,
        Guid userId,
        Guid clinicId,
        ClinicRole role,
        CancellationToken cancellationToken)
    {
        var membership = await dbContext.ClinicMemberships
            .FirstOrDefaultAsync(
                item => item.UserId == userId && item.ClinicId == clinicId,
                cancellationToken);
        if (membership is null)
        {
            dbContext.ClinicMemberships.Add(new ClinicMembership(userId, clinicId, role));
        }
        else if (!membership.IsActive || membership.Role != role)
        {
            membership.Activate();
            membership.ChangeRole(role);
        }
    }
}
