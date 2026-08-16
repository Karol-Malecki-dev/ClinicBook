using Application.DTOs.Patient;
using FluentValidation;

namespace API.Validators.Patient;

public sealed class CreatePatientProfileRequestDtoValidator : AbstractValidator<CreatePatientProfileRequestDto>
{
    public CreatePatientProfileRequestDtoValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(request => request.PhoneNumber)
            .MaximumLength(50)
            .Must(phoneNumber => !string.IsNullOrWhiteSpace(phoneNumber))
            .When(request => request.PhoneNumber is not null);

        RuleFor(request => request.DateOfBirth)
            .Must(dateOfBirth => dateOfBirth <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.");

        RuleFor(request => request.Address)
            .SetValidator(new API.Validators.AddressDtoValidator())
            .When(request => request.Address is not null);
    }
}
