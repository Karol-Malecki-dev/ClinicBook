using Application.DTOs.Clinic;
using Application.Mappings;
using AutoMapper;
using Domain.Entities.Clinic;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Mappings;

public sealed class ClinicMappingProfileTests
{
    private readonly IMapper _mapper;

    public ClinicMappingProfileTests()
    {
        var configuration = new MapperConfiguration(
            configuration => configuration.AddMaps(typeof(AutoMapperProfile).Assembly),
            NullLoggerFactory.Instance);

        configuration.AssertConfigurationIsValid();
        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public void Should_have_valid_auto_mapper_configuration()
    {
        var configuration = _mapper.ConfigurationProvider;

        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void Should_map_clinic_to_response_dto()
    {
        var address = new Address
        {
            Street = "Wojska Polskiego",
            BuildingNumber = "12",
            ApartmentNumber = "3",
            City = "Olsztyn",
            PostalCode = "10-001",
            Country = "Poland"
        };
        var clinic = new Clinic(
            "Clinic One",
            address,
            new TimeOnly(8, 0),
            new TimeOnly(16, 0));

        clinic.UpdateDetails(
            clinic.Name,
            clinic.Address,
            clinic.OpenTime,
            clinic.CloseTime,
            "+48123123123",
            "+48456456456",
            null,
            "clinic@example.com",
            "https://clinic.example.com");

        var result = _mapper.Map<ClinicResponseDto>(clinic);

        Assert.Equal(clinic.Id, result.Id);
        Assert.Equal(clinic.Name, result.Name);
        Assert.Equal(clinic.PhoneNumber, result.PhoneNumber);
        Assert.Equal(clinic.PhoneNumber2, result.PhoneNumber2);
        Assert.Equal(clinic.PhoneNumber3, result.PhoneNumber3);
        Assert.Equal(clinic.Email, result.Email);
        Assert.Equal(clinic.Website, result.Website);
        Assert.Equal(clinic.OpenTime, result.OpenTime);
        Assert.Equal(clinic.CloseTime, result.CloseTime);
        Assert.Equal(clinic.Address.Street, result.Address.Street);
        Assert.Equal(clinic.Address.BuildingNumber, result.Address.BuildingNumber);
        Assert.Equal(clinic.Address.ApartmentNumber, result.Address.ApartmentNumber);
        Assert.Equal(clinic.Address.City, result.Address.City);
        Assert.Equal(clinic.Address.PostalCode, result.Address.PostalCode);
        Assert.Equal(clinic.Address.Country, result.Address.Country);
    }
}
