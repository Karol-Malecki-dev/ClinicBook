using Domain.Entities.Clinic;
using Domain.Enums;

namespace UnitTests.Domain.Clinic;

public sealed class DoctorScheduleExceptionTests
{
    [Fact]
    public void Constructor_rejects_non_divisible_additional_availability()
    {
        var action = () => new DoctorScheduleException(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2030, 1, 7),
            ScheduleExceptionType.Available,
            new TimeOnly(9, 0),
            new TimeOnly(9, 45),
            30);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_allows_partial_closure_not_aligned_to_slot_duration()
    {
        var exception = new DoctorScheduleException(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2030, 1, 7),
            ScheduleExceptionType.Closed,
            new TimeOnly(9, 15),
            new TimeOnly(9, 45),
            30,
            "  Training  ");

        Assert.Equal("Training", exception.Reason);
        Assert.Equal(new TimeOnly(9, 15), exception.StartTime);
    }
}
