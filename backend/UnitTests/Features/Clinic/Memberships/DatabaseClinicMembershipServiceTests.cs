using Application.DTOs.ClinicMembership;
using Domain.Entities;
using Domain.Entities.Clinic;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Data;
using Infrastructure.Services.Clinic;
using Microsoft.EntityFrameworkCore;
using ClinicEntity = Domain.Entities.Clinic.Clinic;

namespace UnitTests.Features.Clinic.Memberships;

public sealed class DatabaseClinicMembershipServiceTests
{
    [Fact]
    public async Task CreateClinicMembershipAsync_Allows_clinic_administrator_to_assign_patient_role()
    {
        await using var context = CreateContext();
        var clinic = CreateClinic();
        var administrator = CreateUser();
        var patient = CreateUser();

        context.AddRange(clinic, administrator, patient);
        context.ClinicMemberships.Add(new ClinicMembership(administrator.Id, clinic.Id, ClinicRole.ClinicAdministrator));
        await context.SaveChangesAsync();

        var service = new DatabaseClinicMembershipService(context);
        var result = await service.CreateClinicMembershipAsync(
            administrator.Id,
            clinic.Id,
            new AddClinicMembershipRequestDto { UserId = patient.Id, Role = ClinicRole.Patient });

        Assert.Equal(201, result.StatusCode);
        Assert.Equal(ClinicRole.Patient, result.Data!.Role);
    }

    [Fact]
    public async Task CreateClinicMembershipAsync_Denies_member_without_management_permission()
    {
        await using var context = CreateContext();
        var clinic = CreateClinic();
        var patient = CreateUser();
        var newUser = CreateUser();

        context.AddRange(clinic, patient, newUser);
        context.ClinicMemberships.Add(new ClinicMembership(patient.Id, clinic.Id, ClinicRole.Patient));
        await context.SaveChangesAsync();

        var service = new DatabaseClinicMembershipService(context);
        var result = await service.CreateClinicMembershipAsync(
            patient.Id,
            clinic.Id,
            new AddClinicMembershipRequestDto { UserId = newUser.Id, Role = ClinicRole.Patient });

        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task DeleteClinicMembershipAsync_Cannot_remove_owner_membership()
    {
        await using var context = CreateContext();
        var clinic = CreateClinic();
        var administrator = CreateUser();
        var owner = CreateUser();

        context.AddRange(clinic, administrator, owner);
        context.ClinicMemberships.AddRange(
            new ClinicMembership(administrator.Id, clinic.Id, ClinicRole.ClinicAdministrator),
            new ClinicMembership(owner.Id, clinic.Id, ClinicRole.Owner));
        await context.SaveChangesAsync();

        var service = new DatabaseClinicMembershipService(context);
        var result = await service.DeleteClinicMembershipAsync(administrator.Id, clinic.Id, owner.Id);

        Assert.Equal(400, result.StatusCode);
        Assert.True(await context.ClinicMemberships.AnyAsync(item => item.UserId == owner.Id && item.IsActive));
    }

    [Fact]
    public async Task GetClinicMembershipsAsync_Denies_user_from_another_clinic()
    {
        await using var context = CreateContext();
        var accessibleClinic = CreateClinic();
        var otherClinic = CreateClinic();
        var user = CreateUser();

        context.AddRange(accessibleClinic, otherClinic, user);
        context.ClinicMemberships.Add(new ClinicMembership(user.Id, accessibleClinic.Id, ClinicRole.Patient));
        await context.SaveChangesAsync();

        var service = new DatabaseClinicMembershipService(context);
        var result = await service.GetClinicMembershipsAsync(user.Id, otherClinic.Id);

        Assert.Equal(403, result.StatusCode);
        Assert.Null(result.Data);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static ClinicEntity CreateClinic()
        => new(
            "Test Clinic",
            new Address
            {
                Street = "Main Street",
                BuildingNumber = "1",
                City = "Olsztyn",
                PostalCode = "10-001",
                Country = "Poland"
            },
            new TimeOnly(8, 0),
            new TimeOnly(16, 0));

    private static User CreateUser()
        => new()
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid()}@example.com",
            DisplayName = "Test User",
            IsActive = true,
            Role = UserRole.User
        };
}
