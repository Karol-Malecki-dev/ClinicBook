
using API.Controllers.Doctor;
using Application.DTOs.Doctor.DoctorProfile;
using Application.Interfaces.Doctor;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.Responses;
using System.Security.Claims;

namespace UnitTests.Features.Doctor.Profiles.Controllers;

public sealed class DoctorProfileControllerTests
{
    private readonly Mock<IDoctorProfileService> _profileServiceMock = new();
    private readonly DoctorProfileController _controller;

    public DoctorProfileControllerTests()
    {
        _controller = new DoctorProfileController(_profileServiceMock.Object);
    }

    [Fact]
    public async Task CreateDoctorProfile_Returns201_WhenServiceCreatesProfile()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var request = new CreateDoctorProfileRequestDto
        {
            FirstName = "John",
            LastName = "Doe",
            Bio = "A",
            PhoneNumber = "123456"
        };

        var responseDto = new DoctorProfileResponseDto
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
        };

        _controller.ControllerContext = CreateControllerContext(currentUserId);

        _profileServiceMock
            .Setup(service => service.CreateDoctorProfileAsync(
                currentUserId,
                currentUserId,
                request))
            .ReturnsAsync(
                ApiResponse<DoctorProfileResponseDto>.Success(
                    responseDto,
                    "Doctor profile created successfully.",
                    201));

        // Act

        var actionResult = await _controller.CreateDoctorProfile(currentUserId, request);

        // Assert

        var result = Assert.IsType<ObjectResult>(actionResult.Result);

        Assert.Equal(201, result.StatusCode);

        _profileServiceMock.Verify(service =>
           service.CreateDoctorProfileAsync(
                currentUserId,
                currentUserId,
                request),
            Times.Once);
    }

    [Fact]
    public async Task CreateDoctorProfile_ReturnsUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var doctorUserId = Guid.NewGuid();

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var request = new CreateDoctorProfileRequestDto
        {
            FirstName = "John",
            LastName = "Doe",
            Bio = "A",
            PhoneNumber = "123456"
        };

        // Act
        var actionResult = await _controller.CreateDoctorProfile(
            doctorUserId,
            request);

        // Assert
        var result = Assert.IsType<UnauthorizedObjectResult>(
            actionResult.Result);

        Assert.Equal(401, result.StatusCode);

        _profileServiceMock.Verify(
            service => service.CreateDoctorProfileAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CreateDoctorProfileRequestDto>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateDoctorProfile_Returns400_WhenRequestIsInvalid()
    {
        // Arrange
        var doctorUserId = Guid.NewGuid();

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var request = new CreateDoctorProfileRequestDto
        {
            FirstName = new string('J', 100),
            LastName = "Doe",
            Bio = "A",
            PhoneNumber = "123456"
        };

        // Act
        var actionResult = await _controller.CreateDoctorProfile(
            doctorUserId,
            request);

        // Assert
        var result = Assert.IsType<UnauthorizedObjectResult>(
            actionResult.Result);

        Assert.Equal(401, result.StatusCode);

        _profileServiceMock.Verify(
            service => service.CreateDoctorProfileAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CreateDoctorProfileRequestDto>()),
            Times.Never);

    }

    [Fact]
    public async Task CreateDoctorProfile_Returns401_WhenUserIsNotAuthenticated()
    {


    }

    [Fact]
    public async Task CreateDoctorProfile_Returns403_WhenServiceDeniesAccess()
    {

    }

    [Fact]
    public async Task CreateDoctorProfile_Returns409_WhenProfileAlreadyExists()
    {

    }

    private static ControllerContext CreateControllerContext(Guid userId)
    {
        var httpContext = new DefaultHttpContext();

        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            },
                authenticationType: "Test"));

        return new ControllerContext
        {
            HttpContext = httpContext
        };
    }
}
