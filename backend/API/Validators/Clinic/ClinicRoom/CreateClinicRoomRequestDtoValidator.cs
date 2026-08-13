using Application.DTOs.Clinic.ClinicRoom;
using FluentValidation;

namespace API.Validators.Clinic.ClinicRoom
{
    public class CreateClinicRoomRequestDtoValidator : AbstractValidator<CreateClinicRoomRequestDto>
    {
        public CreateClinicRoomRequestDtoValidator()
        {
            RuleFor(request => request.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(request => request.Description)
                .MaximumLength(500);
        }


    }
}
