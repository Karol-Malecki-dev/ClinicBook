
using API.Validators.Patient;
using Application.DTOs.Other;
using Application.DTOs.Patient;
using FluentValidation.TestHelper;

namespace UnitTests.Validators.Patient
{
    public sealed class UpdatePatientProfileDtoValidatorTests
    {
        private readonly UpdatePatientProfileRequestDtoValidator _validator = new();

        [Fact]
        public void Should_have_error_when_name_is_empty()
        {
            var dto = CreateDto(string.Empty, new DateOnly(1990, 1, 1), "+48123123123", CreateValidAddress());

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_not_have_errors_when_update_request_is_valid()
        {
            var result = _validator.TestValidate(
                CreateDto("Jan Kowalski", new DateOnly(1990, 1, 1), "+48123123123", CreateValidAddress()));

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_have_error_when_name_is_too_long()
        {
            var dto = CreateDto(new string('K', 257), new DateOnly(1990, 1, 1), "+48123123123", CreateValidAddress());

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_have_error_when_date_of_birth_is_in_the_future()
        {
            var dto = CreateDto("Jan Kowalski", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), "+48123123123", CreateValidAddress());

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
        }

        [Fact]
        public void Should_have_error_when_phone_number_contains_only_whitespace()
        {
            var dto = CreateDto("Jan Kowalski", new DateOnly(1990, 1, 1), "   ", CreateValidAddress());

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
        }

        [Fact]
        public void Should_have_error_when_address_is_invalid()
        {
            var dto = CreateDto("Jan Kowalski", new DateOnly(1990, 1, 1), "+48123123123", new AddressDto
            {
                Street = null!,
                BuildingNumber = "12",
                City = "Olsztyn",
                PostalCode = "10-001",
                Country = "Poland"
            });

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor("Address.Street");
        }

        [Fact]
        public void Should_not_have_error_when_phone_number_and_address_are_null()
        {
            var result = _validator.TestValidate(
                CreateDto("Jan Kowalski", new DateOnly(1990, 1, 1), null, null));

            result.ShouldNotHaveAnyValidationErrors();
        }

        private static UpdatePatientProfileRequestDto CreateDto(
            string name,
            DateOnly dateOfBirth,
            string? phoneNumber,
            AddressDto? address)
        {
            return new UpdatePatientProfileRequestDto
            {
                Name = name,
                DateOfBirth = dateOfBirth,
                PhoneNumber = phoneNumber,
                Address = address
            };
        }

        private static AddressDto CreateValidAddress()
        {
            return new AddressDto
            {
                Street = "Wojska Polskiego",
                BuildingNumber = "12",
                ApartmentNumber = "3",
                City = "Olsztyn",
                PostalCode = "10-001",
                Country = "Poland"
            };
        }
    }
}
