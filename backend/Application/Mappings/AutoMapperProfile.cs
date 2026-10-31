using AutoMapper;
using Application.DTOs.Clinic;
using Application.DTOs.Other;
using Domain.Entities.Clinic;
using Domain.ValueObjects;

namespace Application.Mappings;

/// <summary>
/// Base profile for mappings that are introduced with new application features.
/// </summary>
public sealed class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Address, AddressDto>();
        CreateMap<Clinic, ClinicResponseDto>()
            .ForMember(destination => destination.MembershipRole, options => options.Ignore());
    }
}
