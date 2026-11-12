using FluentAssertions;
using Prixo.Application.Features.Appointments;

namespace Prixo.Application.Tests.Appointments;

public class ReplaceUnitScheduleRequestValidatorTests
{
    private readonly ReplaceUnitScheduleRequestValidator _validator = new();

    [Fact]
    public void NonOverlappingBlocks_Pass()
    {
        var request = new ReplaceUnitScheduleRequest(
        [
            new SaveUnitScheduleBlockRequest(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)),
            new SaveUnitScheduleBlockRequest(DayOfWeek.Monday, new TimeOnly(14, 0), new TimeOnly(17, 0)),
        ]);
        _validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void OverlappingBlocksOnSameDay_Fail()
    {
        var request = new ReplaceUnitScheduleRequest(
        [
            new SaveUnitScheduleBlockRequest(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)),
            new SaveUnitScheduleBlockRequest(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(14, 0)),
        ]);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EndBeforeStart_Fails()
    {
        var request = new ReplaceUnitScheduleRequest(
            [new SaveUnitScheduleBlockRequest(DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(9, 0))]);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }
}
