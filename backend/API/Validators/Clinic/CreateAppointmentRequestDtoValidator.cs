using Application.DTOs.Clinic;
using FluentValidation;

namespace API.Validators.Clinic;

public sealed class CreateAppointmentRequestDtoValidator
    : AbstractValidator<CreateAppointmentRequestDto>
{
    public CreateAppointmentRequestDtoValidator()
    {
        RuleFor(request => request.StartsAtUtc)
            .NotEqual(default(DateTimeOffset));
    }
}
