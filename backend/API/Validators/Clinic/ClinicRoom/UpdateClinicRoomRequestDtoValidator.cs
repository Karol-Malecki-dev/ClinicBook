using Application.DTOs.ClinicRoom;
using FluentValidation;

namespace API.Validators.Clinic.ClinicRoom;

public sealed class UpdateClinicRoomRequestDtoValidator
    : AbstractValidator<UpdateClinicRoomRequestDto>
{
    public UpdateClinicRoomRequestDtoValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.Description)
            .MaximumLength(500);
    }
}
