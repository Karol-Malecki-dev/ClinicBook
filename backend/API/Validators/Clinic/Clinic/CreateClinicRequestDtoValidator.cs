using Application.DTOs.Clinic;
using API.Validators.Clinic;
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
            .MaximumLength(256);

        RuleFor(request => request.PhoneNumber2)
            .MaximumLength(256);

        RuleFor(request => request.PhoneNumber3)
            .MaximumLength(256);

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

        RuleFor(request => request.TimeZoneId)
            .NotEmpty()
            .MaximumLength(100)
            .Must(ClinicTimeZoneValidation.IsSupported)
            .WithMessage("A supported clinic time zone is required.");
    }

}
