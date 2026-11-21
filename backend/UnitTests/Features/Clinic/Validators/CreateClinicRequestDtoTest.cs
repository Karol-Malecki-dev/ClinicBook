using API.Validators.Clinic.Clinic;
using Application.DTOs.Clinic;
using Application.DTOs.Other;
using FluentValidation.TestHelper;

namespace UnitTests.Validators.Clinic
{
    public sealed class CreateClinicRequestDtoTest
    {
        private readonly CreateClinicRequestDtoValidator _validator = new();

        [Fact]
        public async Task Should_have_error_when_name_is_empty()
        {
            var dto = new CreateClinicRequestDto
            {
                Name = string.Empty,
                Address = new AddressDto
                {
                    Street = "123 Main St",
                    City = "Anytown",
                    BuildingNumber = "123",
                    ApartmentNumber = "12345",
                    PostalCode = "12345",
                    Country = "USA"
                },
                PhoneNumber = "123-456-7890",
                PhoneNumber2 = "123-456-7890",
                PhoneNumber3 = "123-456-7890",
                Email = "test@example.com",
                Website = "https://example.com",
                OpenTime = new TimeOnly(9, 0),
                CloseTime = new TimeOnly(17, 0)
            };

            var result = await _validator.TestValidateAsync(dto);

            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public async Task Should_have_error_when_name_too_long()
        {
            var dto = new CreateClinicRequestDto
            {
                Name = new string('K',500),
                Address = new AddressDto
                {
                    Street = "123 Main St",
                    City = "Anytown",
                    BuildingNumber = "123",
                    ApartmentNumber = "12345",
                    PostalCode = "12345",
                    Country = "USA"
                },
                PhoneNumber = "123-456-7890",
                PhoneNumber2 = "123-456-7890",
                PhoneNumber3 = "123-456-7890",
                Email = "test@example.com",
                Website = "https://example.com",
                OpenTime = new TimeOnly(9, 0),
                CloseTime = new TimeOnly(17, 0)
            };

            var result = await _validator.TestValidateAsync(dto);

            result.ShouldHaveValidationErrorFor(x => x.Name);
        }


        [Fact]
        public async Task Should_have_error_when_Address_is_null()
        {
            var dto = new CreateClinicRequestDto
            {
                Name = "Karol",
                Address = null,
                PhoneNumber = null,
                PhoneNumber2 = "123-456-7890",
                PhoneNumber3 = "123-456-7890",
                Email = "test@example.com",
                Website = "https://example.com",
                OpenTime = new TimeOnly(9, 0),
                CloseTime = new TimeOnly(17, 0)
            };

            var result = await _validator.TestValidateAsync(dto);

            result.ShouldHaveValidationErrorFor(x => x.Address);
        }

        [Fact]
        public async Task Should_have_error_when_phoneNumber_is_whiteSpace()
        {
            var dto = new CreateClinicRequestDto
            {
                Name = "Karol Clinic",
                Address = new AddressDto
                {
                    Street = "123 Main St",
                    City = "Anytown",
                    BuildingNumber = "123",
                    ApartmentNumber = "12345",
                    PostalCode = "12345",
                    Country = "USA"
                },
                PhoneNumber = "   ",
                PhoneNumber2 = "123-456-7890",
                PhoneNumber3 = "123-456-7890",
                Email = "test@example.com",
                Website = "https://example.com",
                OpenTime = new TimeOnly(9, 0),
                CloseTime = new TimeOnly(17, 0)
            };

            var result = await _validator.TestValidateAsync(dto);

            result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
        }

        [Fact]
        public void Should_not_have_errors_when_create_request_is_valid()
        {
            var result = _validator.TestValidate(CreateValidDto());

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_have_error_when_email_is_invalid()
        {
            var result = _validator.TestValidate(CreateValidDto(email: "invalid-email"));

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Should_have_error_when_website_is_invalid()
        {
            var result = _validator.TestValidate(CreateValidDto(website: "invalid-website"));

            result.ShouldHaveValidationErrorFor(x => x.Website);
        }

        [Fact]
        public void Should_have_error_when_close_time_is_not_after_open_time()
        {
            var result = _validator.TestValidate(
                CreateValidDto(openTime: new TimeOnly(9, 0), closeTime: new TimeOnly(9, 0)));

            result.ShouldHaveValidationErrorFor(x => x.CloseTime);
        }

        private static CreateClinicRequestDto CreateValidDto(
            string email = "test@example.com",
            string website = "https://example.com",
            TimeOnly? openTime = null,
            TimeOnly? closeTime = null)
        {
            return new CreateClinicRequestDto
            {
                Name = "Karol Clinic",
                Address = new AddressDto
                {
                    Street = "123 Main St",
                    City = "Anytown",
                    BuildingNumber = "123",
                    ApartmentNumber = "12345",
                    PostalCode = "12345",
                    Country = "USA"
                },
                PhoneNumber = "123-456-7890",
                PhoneNumber2 = "123-456-7890",
                PhoneNumber3 = "123-456-7890",
                Email = email,
                Website = website,
                OpenTime = openTime ?? new TimeOnly(9, 0),
                CloseTime = closeTime ?? new TimeOnly(17, 0)
            };
        }

    }
}
