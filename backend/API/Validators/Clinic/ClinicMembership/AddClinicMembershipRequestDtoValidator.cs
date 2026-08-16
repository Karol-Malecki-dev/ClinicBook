using Application.DTOs.ClinicMembership;
using FluentValidation;

namespace API.Validators.Clinic.ClinicMembership;

public sealed class AddClinicMembershipRequestDtoValidator
    : AbstractValidator<AddClinicMembershipRequestDto>
{
    public AddClinicMembershipRequestDtoValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty();

        RuleFor(request => request.Role)
            .IsInEnum();
    }
}
