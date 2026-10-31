using Application.DTOs.ClinicMembership;
using FluentValidation;

namespace API.Validators.Clinic.ClinicMembership;

public sealed class AddClinicMembershipRequestDtoValidator
    : AbstractValidator<AddClinicMembershipRequestDto>
{
    public AddClinicMembershipRequestDtoValidator()
    {
        RuleFor(request => request)
            .Must(request =>
                (request.UserId != Guid.Empty)
                != !string.IsNullOrWhiteSpace(request.Email))
            .WithMessage("Provide either a user ID or an email address, but not both.");

        RuleFor(request => request.Role)
            .IsInEnum();

        RuleFor(request => request.Email)
            .EmailAddress()
            .When(request => !string.IsNullOrWhiteSpace(request.Email));

        RuleFor(request => request.Email)
            .MaximumLength(256);
    }
}
