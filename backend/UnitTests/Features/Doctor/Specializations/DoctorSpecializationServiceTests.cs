using Infrastructure.Services.Doctor;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Features.Doctor.Specializations;

public sealed class DoctorSpecializationServiceTests
{
    [Fact]
    public async Task GetDoctorSpecializationsAsync_Returns_bad_request_when_current_user_id_is_empty()
    {
        await using var context = CreateContext();
        var service = new DatabaseDoctorSpecializationService(context);

        var result = await service.GetDoctorSpecializationsAsync(Guid.Empty, Guid.NewGuid());

        Assert.Equal(400, result.StatusCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetDoctorSpecializationsAsync_Returns_not_found_when_doctor_profile_does_not_exist()
    {
        await using var context = CreateContext();
        var service = new DatabaseDoctorSpecializationService(context);

        var result = await service.GetDoctorSpecializationsAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(404, result.StatusCode);
        Assert.Null(result.Data);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
