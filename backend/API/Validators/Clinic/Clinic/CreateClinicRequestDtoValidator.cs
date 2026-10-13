using Application.DTOs.Clinic;
using FluentValidation;

namespace API.Validators.Clinic.Clinic;

public sealed class CreateClinicRequestDtoValidator : AbstractValidator<CreateClinicRequestDto>
{
    public CreateClinicRequestDtoValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(request => request.Address)
            .NotNull()
            .SetValidator(new AddressDtoValidator());

        RuleFor(request => request.PhoneNumber)
            .MaximumLength(256)
            .Must(phoneNumber => !string.IsNullOrWhiteSpace(phoneNumber))
            .When(request => request.PhoneNumber is not null);

        RuleFor(request => request.PhoneNumber2)
            .MaximumLength(256)
            .Must(phoneNumber => !string.IsNullOrWhiteSpace(phoneNumber))
            .When(request => request.PhoneNumber2 is not null);

        RuleFor(request => request.PhoneNumber3)
            .MaximumLength(256)
            .Must(phoneNumber => !string.IsNullOrWhiteSpace(phoneNumber))
            .When(request => request.PhoneNumber3 is not null);

        RuleFor(request => request.Email)
            .MaximumLength(256);

        RuleFor(request => request.Email)
            .EmailAddress()
            .When(request => !string.IsNullOrWhiteSpace(request.Email));

        RuleFor(request => request.Website)
            .MaximumLength(256);

        RuleFor(request => request.Website)
            .Must(WebsiteValidation.IsValid)
            .When(request => !string.IsNullOrWhiteSpace(request.Website));

        RuleFor(request => request.CloseTime)
            .GreaterThan(request => request.OpenTime);
    }

}
