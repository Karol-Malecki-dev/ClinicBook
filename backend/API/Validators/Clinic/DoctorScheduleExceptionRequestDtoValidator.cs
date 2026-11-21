using Application.DTOs.Clinic;
using Domain.Enums;
using FluentValidation;

namespace API.Validators.Clinic;

public sealed class DoctorScheduleExceptionRequestDtoValidator
    : AbstractValidator<CreateDoctorScheduleExceptionRequestDto>
{
    public DoctorScheduleExceptionRequestDtoValidator()
    {
        RuleFor(request => request.Date)
            .Must(date => date != default)
            .WithMessage("Exception date is required.");

        RuleFor(request => request.Type)
            .IsInEnum();

        RuleFor(request => request.EndTime)
            .Must((request, endTime) => endTime > request.StartTime)
            .WithMessage("Schedule exception must end after it starts.");

        RuleFor(request => request.SlotDurationMinutes)
            .InclusiveBetween(15, 120)
            .Must(duration => duration % 15 == 0)
            .WithMessage("Slot duration must use 15-minute increments.");

        RuleFor(request => request)
            .Must(request =>
            {
                if (request.Type != ScheduleExceptionType.Available
                    || request.SlotDurationMinutes <= 0
                    || request.EndTime <= request.StartTime)
                {
                    return true;
                }

                return (request.EndTime - request.StartTime).TotalMinutes
                    % request.SlotDurationMinutes == 0;
            })
            .WithMessage("Additional availability length must be divisible by the slot duration.");

        RuleFor(request => request.Reason)
            .MaximumLength(300);
    }
}
