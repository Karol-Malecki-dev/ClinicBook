using API.Validators;
using Application.DTOs.Other;
using FluentValidation.TestHelper;
using Xunit;

namespace UnitTests.Validators
{
    public sealed class AddressDtoValidatorTest
    {
        private readonly AddressDtoValidator _validator = new();

        [Fact]
        public void Should_have_error_when_Street_empty()
        {
            var dto = new AddressDto {
                Street =  null,
                BuildingNumber = "123",
                ApartmentNumber = "123",
                City = "Olsztyn",
                PostalCode = "2133213",
                Country = " Poland" };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Street);
        }

        [Fact]
        public void Should_have_error_when_Street_is_too_long()
        {
            var dto = new AddressDto
            {
                Street = new string('k', 151),
                BuildingNumber = "123",
                ApartmentNumber = "123",
                City = "Olsztyn",
                PostalCode = "2133213",
                Country = " Poland"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Street);
        }

        [Fact]
        public void Should_have_error_when_BuildingNumber_empty()
        {
            var dto = new AddressDto
            {
                Street = "Wojska Polskiego",
                BuildingNumber = null,
                ApartmentNumber = "123",
                City = "Olsztyn",
                PostalCode = "2133213",
                Country = " Poland"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.BuildingNumber);
        }

        [Fact]
        public void Should_have_error_when_BuildingNumber_is_too_long()
        {
            var dto = new AddressDto
            {
                Street = "Wojska Polskiego",
                BuildingNumber = new string('k', 21),
                ApartmentNumber = "123",
                City = "Olsztyn",
                PostalCode = "2133213",
                Country = " Poland"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.BuildingNumber);
        }

        [Fact]
        public void Should_not_have_error_when_ApartmentNumber_is_null()
        {
            var dto = new AddressDto
            {
                Street = "Wojska Polskiego",
                BuildingNumber = "123",
                ApartmentNumber = null,
                City = "Olsztyn",
                PostalCode = "2133213",
                Country = " Poland"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.ApartmentNumber);
        }

        [Fact]
        public void Should_have_error_when_ApartmentNumber_is_too_long()
        {
            var dto = new AddressDto
            {
                Street = "Wojska Polskiego",
                BuildingNumber = "123",
                ApartmentNumber = new string('k', 21),
                City = "Olsztyn",
                PostalCode = "2133213",
                Country = " Poland"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.ApartmentNumber);
        }

        [Fact]
        public void Should_have_error_when_City_empty()
        {
            var dto = new AddressDto
            {
                Street = "Wojska Polskiego",
                BuildingNumber = "123",
                ApartmentNumber = "123",
                City = null,
                PostalCode = "2133213",
                Country = " Poland"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.City);
        }

        [Fact]
        public void Should_have_error_when_City_is_too_long()
        {
            var dto = new AddressDto
            {
                Street = "Wojska Polskiego",
                BuildingNumber = "123",
                ApartmentNumber = "123",
                City = new string('k', 101),
                PostalCode = "2133213",
                Country = " Poland"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.City);
        }

        [Fact]
        public void Should_have_error_when_PostalCode_empty()
        {
            var dto = new AddressDto
            {
                Street = "Wojska Polskiego",
                BuildingNumber = "123",
                ApartmentNumber = "123",
                City = "Olsztyn",
                PostalCode = null,
                Country = " Poland"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.PostalCode);
        }

        [Fact]
        public void Should_have_error_when_PostalCode_is_too_long()
        {
            var dto = new AddressDto
            {
                Street = "Wojska Polskiego",
                BuildingNumber = "123",
                ApartmentNumber = "123",
                City = "Olsztyn",
                PostalCode = new string('k', 21),
                Country = " Poland"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.PostalCode);
        }

        [Fact]
        public void Should_have_error_when_Country_empty()
        {
            var dto = new AddressDto
            {
                Street = "Wojska Polskiego",
                BuildingNumber = "123",
                ApartmentNumber = "123",
                City = "Olsztyn",
                PostalCode = "2133213",
                Country = null
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Country);
        }

        [Fact]
        public void Should_have_error_when_Country_is_too_long()
        {
            var dto = new AddressDto
            {
                Street = "Wojska Polskiego",
                BuildingNumber = "123",
                ApartmentNumber = "123",
                City = "Olsztyn",
                PostalCode = "2133213",
                Country = new string('k', 151)
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Country);
        }
    }
}
