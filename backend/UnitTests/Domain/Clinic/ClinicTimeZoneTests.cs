using Domain.ValueObjects;
using ClinicEntity = Domain.Entities.Clinic.Clinic;

namespace UnitTests.Domain.Clinic;

public sealed class ClinicTimeZoneTests
{
    [Fact]
    public void Constructor_uses_the_default_clinic_time_zone()
    {
        var clinic = CreateClinic();

        Assert.Equal(ClinicEntity.DefaultTimeZoneId, clinic.TimeZoneId);
    }

    [Fact]
    public void Constructor_trims_and_stores_a_supported_time_zone_id()
    {
        var clinic = CreateClinic(timeZoneId: " Europe/Warsaw ");

        Assert.Equal("Europe/Warsaw", clinic.TimeZoneId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Not/A-Time-Zone")]
    public void Constructor_rejects_an_empty_or_unknown_time_zone(string timeZoneId)
    {
        Assert.Throws<ArgumentException>(() => CreateClinic(timeZoneId: timeZoneId));
    }

    [Fact]
    public void UpdateDetails_keeps_the_existing_time_zone_when_no_new_zone_is_provided()
    {
        var clinic = CreateClinic(timeZoneId: "Europe/Warsaw");

        clinic.UpdateDetails(
            "Updated clinic",
            CreateAddress(),
            new TimeOnly(8, 0),
            new TimeOnly(18, 0));

        Assert.Equal("Europe/Warsaw", clinic.TimeZoneId);
    }

    [Fact]
    public void UpdateDetails_rejects_an_unknown_time_zone_without_changing_the_existing_zone()
    {
        var clinic = CreateClinic(timeZoneId: "Europe/Warsaw");

        Assert.Throws<ArgumentException>(() => clinic.UpdateDetails(
            "Updated clinic",
            CreateAddress(),
            new TimeOnly(8, 0),
            new TimeOnly(18, 0),
            timeZoneId: "Not/A-Time-Zone"));

        Assert.Equal("Europe/Warsaw", clinic.TimeZoneId);
    }

    private static ClinicEntity CreateClinic(string timeZoneId = ClinicEntity.DefaultTimeZoneId)
    {
        return new ClinicEntity(
            "Test clinic",
            CreateAddress(),
            new TimeOnly(8, 0),
            new TimeOnly(17, 0),
            timeZoneId);
    }

    private static Address CreateAddress()
    {
        return new Address
        {
            Street = "Main Street",
            BuildingNumber = "1",
            City = "Warsaw",
            PostalCode = "00-001",
            Country = "Poland"
        };
    }
}
