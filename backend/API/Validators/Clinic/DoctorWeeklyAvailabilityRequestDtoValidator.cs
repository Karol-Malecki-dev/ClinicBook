using Application.DTOs.Clinic;
using FluentValidation;

namespace API.Validators.Clinic;

public sealed class SetDoctorWeeklyAvailabilityRequestDtoValidator
    : AbstractValidator<SetDoctorWeeklyAvailabilityRequestDto>
{
    public SetDoctorWeeklyAvailabilityRequestDtoValidator()
    {
        RuleFor(request => request.DayOfWeek)
            .IsInEnum();

        RuleFor(request => request.EndTime)
            .Must((request, endTime) => endTime > request.StartTime)
            .WithMessage("Availability must end after it starts.");

        RuleFor(request => request.SlotDurationMinutes)
            .InclusiveBetween(15, 120)
            .Must(duration => duration % 15 == 0)
            .WithMessage("Slot duration must use 15-minute increments.");

        RuleFor(request => request)
            .Must(request =>
            {
                if (request.SlotDurationMinutes <= 0 || request.EndTime <= request.StartTime)
                {
                    return true;
                }

                var windowMinutes = (request.EndTime - request.StartTime).TotalMinutes;
                return windowMinutes % request.SlotDurationMinutes == 0;
            })
            .WithMessage("Availability window length must be divisible by the slot duration.");
    }
}
