using FluentValidation;

namespace Prixo.Application.Features.Appointments;

public class SaveUnitScheduleBlockRequestValidator : AbstractValidator<SaveUnitScheduleBlockRequest>
{
    public SaveUnitScheduleBlockRequestValidator()
    {
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time.");
    }
}

public class ReplaceUnitScheduleRequestValidator : AbstractValidator<ReplaceUnitScheduleRequest>
{
    public ReplaceUnitScheduleRequestValidator()
    {
        RuleForEach(x => x.Blocks).SetValidator(new SaveUnitScheduleBlockRequestValidator());
        RuleFor(x => x.Blocks).Must(NotOverlap)
            .WithMessage("Two blocks on the same day must not overlap.");
    }

    private static bool NotOverlap(IReadOnlyList<SaveUnitScheduleBlockRequest> blocks)
    {
        foreach (var group in blocks.GroupBy(b => b.DayOfWeek))
        {
            var ordered = group.OrderBy(b => b.StartTime).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].StartTime < ordered[i - 1].EndTime)
                {
                    return false;
                }
            }
        }
        return true;
    }
}
