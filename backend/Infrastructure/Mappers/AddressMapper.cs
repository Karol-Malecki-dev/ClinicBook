using Application.DTOs.Other;
using Domain.ValueObjects;

namespace Infrastructure.Mappers
{
    internal static class AddressMapper
    {
        internal static AddressDto ToResponseDto(Address address)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));

            return new AddressDto
            {
                Street = address.Street,
                BuildingNumber = address.BuildingNumber,
                ApartmentNumber = address.ApartmentNumber,
                City = address.City,
                PostalCode = address.PostalCode,
                Country = address.Country,
            };
        }
        internal static Address ToAddress(AddressDto address)
        {
            ArgumentNullException.ThrowIfNull(address);

            return new Address
               {
                Street = address.Street,
                BuildingNumber = address.BuildingNumber,
                ApartmentNumber = address.ApartmentNumber,
                City = address.City,
                PostalCode = address.PostalCode,
                Country = address.Country
            };
        }
    }
}
