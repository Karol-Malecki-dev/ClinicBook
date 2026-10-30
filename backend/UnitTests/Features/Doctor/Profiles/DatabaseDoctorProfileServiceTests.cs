using Application.DTOs.Doctor.DoctorProfile;
using Domain.Entities;
using Domain.Entities.Clinic;
using Domain.Entities.Doctor;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Data;
using Infrastructure.Services.Doctor;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Services.Doctor;

public sealed class DatabaseDoctorProfileServiceTests
{
    [Fact]
    public async Task GetDoctorProfileAsync_Denies_access_to_another_users_profile()
    {
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var currentUser = CreateUser("current@example.com");
        var doctorUser = CreateUser("doctor@example.com");
        
        context.Users.AddRange(currentUser, doctorUser);
        context.DoctorProfiles.Add(new DoctorProfile(
            doctorUser.Id,
            "Jane",
            "Doctor",
            "Profile",
            "123456789"));

        await context.SaveChangesAsync();

        var service = new DatabaseDoctorProfileService(context);
        var result = await service.GetDoctorProfileAsync(currentUser.Id, doctorUser.Id);

        Assert.Equal(403, result.StatusCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task CreateDoctorProfileAsync_ReturnsForbidden_WhenUserHasNoActiveDoctorMembership()
    {
        // Arrange
        await using var context = CreateContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "doctor@example.com",
            DisplayName = "Test Doctor",
            IsActive = true,
            Role = UserRole.User
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new DatabaseDoctorProfileService(context);

        var request = new CreateDoctorProfileRequestDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Bio = "Cardiology",
            PhoneNumber = "123456789"
        };

        // Act
        var result = await service.CreateDoctorProfileAsync(
            user.Id,
            user.Id,
            request);

        // Assert
        Assert.False(
            await context.DoctorProfiles.AnyAsync(
                dp => dp.UserId == user.Id));

        Assert.Equal(403, result.StatusCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task CreateDoctorProfileAsync_CreatesProfile_WhenUserHasActiveDoctorMembership()
    {
        //Arrange
        await using var context = CreateContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "doctor@example.com",
            DisplayName = "Test Doctor",
            IsActive = true,
            Role = UserRole.User
        };

        var clinic = CreateClinic();
        context.Users.Add(user);
        context.Clinics.Add(clinic);
        context.ClinicMemberships.Add(new ClinicMembership(user.Id, clinic.Id, ClinicRole.Doctor));
        await context.SaveChangesAsync();

        var service = new DatabaseDoctorProfileService(context);

        var request = new CreateDoctorProfileRequestDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Bio = "Cardiology",
            PhoneNumber = "123456789"
        };
    
        //Act
        var result = await service.CreateDoctorProfileAsync(
            user.Id,
            user.Id,
            request);

        //Assert
        Assert.True(
            await context.DoctorProfiles.AnyAsync(
                dp => dp.UserId == user.Id));

        Assert.Equal(201, result.StatusCode);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task CreateDoctorProfileAsync_ReturnsNotFound_WhenUserDoesNotExist()
    {
        //Arrange
        await using var context = CreateContext();

        var user = CreateUser("doctor@example.com");

        var service = new DatabaseDoctorProfileService(context);

        var request = new CreateDoctorProfileRequestDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Bio = "Cardiology",
            PhoneNumber = "123456789"
        };

        //Act

        var result = await service.CreateDoctorProfileAsync(
            user.Id,
            user.Id,
            request);

        //Asert

        Assert.False(
            await context.DoctorProfiles.AnyAsync(
                dp => dp.UserId == user.Id));
        Assert.Equal(404, result.StatusCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task CreateDoctorProfileAsync_ReturnsNotFound_WhenUserIsInactive()
    {
        // Arrange
        await using var context = CreateContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "doctor@example.com",
            DisplayName = "Test Doctor",
            IsActive = false,
            Role = UserRole.User
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new DatabaseDoctorProfileService(context);

        var request = new CreateDoctorProfileRequestDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Bio = "Cardiology",
            PhoneNumber = "123456789"
        };

        // Act
        var result = await service.CreateDoctorProfileAsync(
            user.Id,
            user.Id,
            request);

        // Assert
        Assert.False(
            await context.DoctorProfiles.AnyAsync(
                dp => dp.UserId == user.Id));
        Assert.Equal(404, result.StatusCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task CreateDoctorProfileAsync_ReturnsConflict_WhenProfileAlreadyExists()
    {
        // Arrange
        await using var context = CreateContext();

        var user = CreateUser("doctor@example.com");
        var clinic = CreateClinic();
        context.Users.Add(user);
        context.Clinics.Add(clinic);
        context.ClinicMemberships.Add(new ClinicMembership(user.Id, clinic.Id, ClinicRole.Doctor));
        await context.SaveChangesAsync();

        var existingProfile = CreateDoctorProfile(user.Id);
        context.DoctorProfiles.Add(existingProfile);
        await context.SaveChangesAsync();

        var service = new DatabaseDoctorProfileService(context);

        var request = new CreateDoctorProfileRequestDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Bio = "Cardiology",
            PhoneNumber = "123456789"
        };

        // Act
        var result = await service.CreateDoctorProfileAsync(
            user.Id,
            user.Id,
            request);

        // Assert
        Assert.True(
            await context.DoctorProfiles.AnyAsync(
                dp => dp.UserId == user.Id));
        Assert.Equal(409, result.StatusCode);
        Assert.Null(result.Data);

    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }


    
    private static User CreateUser(string email)
        => new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = "Test User",
            IsActive = true,
            Role = UserRole.User
        };
    
    private static DoctorProfile CreateDoctorProfile(Guid userId)
        => new(userId, "Jane", "Doctor", "Profile", "123456789");

    private static Clinic CreateClinic()
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
}
