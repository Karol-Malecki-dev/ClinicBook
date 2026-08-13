using Application.DTOs.Clinic.ClinicMembership;
using FluentValidation;

namespace API.Validators.Clinic.ClinicMembership;

public sealed class UpdateClinicMembershipRoleRequestDtoValidator
    : AbstractValidator<UpdateClinicMembershipRoleRequestDto>
{
    public UpdateClinicMembershipRoleRequestDtoValidator()
    {
        RuleFor(request => request.Role)
            .IsInEnum();
    }
}
