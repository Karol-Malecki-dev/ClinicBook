using Application.DTOs.Other;
using FluentValidation;

namespace API.Validators;

public sealed class AddressDtoValidator : AbstractValidator<AddressDto>
{
    public AddressDtoValidator()
    {
        RuleFor(address => address.Street)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(address => address.BuildingNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(address => address.ApartmentNumber)
            .MaximumLength(20);

        RuleFor(address => address.City)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(address => address.PostalCode)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(address => address.Country)
            .NotEmpty()
            .MaximumLength(100);
    }
}
