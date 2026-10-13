using Application.DTOs.Doctor.DoctorProfile;
using FluentValidation;
using Org.BouncyCastle.Asn1.Ocsp;

namespace API.Validators.Clinic.Doctor.Profile;

public class CreateDoctorProfileRequestDtoValidator : AbstractValidator<CreateDoctorProfileRequestDto>
{
    public CreateDoctorProfileRequestDtoValidator()
    {
        RuleFor(request => request.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .MaximumLength(100)
            .WithMessage("First name must not exceed 100 characters.");

        RuleFor(request => request.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .MaximumLength(100)
            .WithMessage("Last name must not exceed 100 characters.");

        RuleFor(request => request.Bio)
            .MaximumLength(100)
            .WithMessage("Bio must not exceed 100 characters.");

        RuleFor(request => request.PhoneNumber)
            .MaximumLength(20)
            .WithMessage("Phone number must not exceed 20 characters.");

    }
}
