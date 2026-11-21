using Application.DTOs.ClinicService;
using FluentValidation;

namespace API.Validators.Clinic.ClinicService;

public sealed class UpdateClinicServiceRequestDtoValidator
    : AbstractValidator<UpdateClinicServiceRequestDto>
{
    public UpdateClinicServiceRequestDtoValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(request => request.Address)
            .NotNull()
            .SetValidator(new AddressDtoValidator());

        RuleFor(request => request.Email)
            .MaximumLength(256);

        RuleFor(request => request.Email)
            .EmailAddress()
            .When(request => !string.IsNullOrWhiteSpace(request.Email));
    }
}
