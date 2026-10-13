using Application.DTOs.ClinicRoom;
using FluentValidation;

namespace API.Validators.Clinic.ClinicRoom
{
    public class CreateClinicRoomRequestDtoValidator
        : AbstractValidator<CreateClinicRoomRequestDto>
    {
        public CreateClinicRoomRequestDtoValidator()
        {
            RuleFor(request => request.Name)
                .NotEmpty()
                .WithMessage("Name is required ")
                .MaximumLength(100)
                .WithMessage("Name must not exceed 100 characters");

            RuleFor(request => request.Description)
                .MaximumLength(500)
                .WithMessage("Description must not exceed 500 characters");
        }
    }
}
