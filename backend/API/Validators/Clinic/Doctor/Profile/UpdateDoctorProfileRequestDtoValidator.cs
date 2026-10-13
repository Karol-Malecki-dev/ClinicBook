using Application.DTOs.Doctor.DoctorProfile;
using FluentValidation;

namespace API.Validators.Clinic.Doctor.Profile
{
    public class UpdateDoctorProfileRequestDtoValidator
        : AbstractValidator<UpdateDoctorProfileRequestDto>
    {
        public UpdateDoctorProfileRequestDtoValidator()
        {
            RuleFor(request => request.FirstName)
                .NotEmpty()
                .WithMessage("First name is required")
                .MaximumLength(50)
                .WithMessage("First name must not exceed 50 characters");

            RuleFor(request => request.LastName)
                .NotEmpty()
                .WithMessage("Last name is required")
                .MaximumLength(50)
                .WithMessage("Last name must not exceed 50 characters");

            RuleFor(request => request.Bio)
                .MaximumLength(100)
                .WithMessage("Bio must not exceed 100 characters");

            RuleFor(request => request.PhoneNumber)
                .MaximumLength(20)
                .WithMessage("Phone number must not exceed 20 characters");
        }
    }
}
