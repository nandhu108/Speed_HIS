using FluentValidation;

namespace Prixo.Application.Features.Appointments;

public class SaveDoctorScheduleBlockRequestValidator : AbstractValidator<SaveDoctorScheduleBlockRequest>
{
    public SaveDoctorScheduleBlockRequestValidator()
    {
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time.");
        RuleFor(x => x.SlotMinutes).InclusiveBetween(1, 240);
    }
}

public class ReplaceDoctorScheduleRequestValidator : AbstractValidator<ReplaceDoctorScheduleRequest>
{
    public ReplaceDoctorScheduleRequestValidator()
    {
        RuleForEach(x => x.Blocks).SetValidator(new SaveDoctorScheduleBlockRequestValidator());
        RuleFor(x => x.Blocks).Must(NotOverlap)
            .WithMessage("Two blocks on the same day must not overlap.");
    }

    private static bool NotOverlap(IReadOnlyList<SaveDoctorScheduleBlockRequest> blocks)
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

public class SaveScheduleOverrideRequestValidator : AbstractValidator<SaveScheduleOverrideRequest>
{
    public SaveScheduleOverrideRequestValidator()
    {
        RuleFor(x => x.SlotMinutes).InclusiveBetween(1, 240).When(x => x.SlotMinutes is not null);
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .When(x => x.IsAvailable && x.StartTime is not null && x.EndTime is not null)
            .WithMessage("End time must be after start time.");
        RuleFor(x => x)
            .Must(x => !x.IsAvailable || (x.StartTime is not null && x.EndTime is not null))
            .WithMessage("Provide start and end time for an available-hours override, or mark it unavailable.");
    }
}

public class BookAppointmentRequestValidator : AbstractValidator<BookAppointmentRequest>
{
    public BookAppointmentRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.DoctorId).GreaterThan(0);
        RuleFor(x => x.TeleconsultLink).MaximumLength(500);
    }
}

public class RescheduleAppointmentRequestValidator : AbstractValidator<RescheduleAppointmentRequest>
{
    public RescheduleAppointmentRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class CancelAppointmentRequestValidator : AbstractValidator<CancelAppointmentRequest>
{
    public CancelAppointmentRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class TransferTokenRequestValidator : AbstractValidator<TransferTokenRequest>
{
    public TransferTokenRequestValidator()
    {
        RuleFor(x => x.NewDoctorId).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class SaveTeleconsultLinkRequestValidator : AbstractValidator<SaveTeleconsultLinkRequest>
{
    public SaveTeleconsultLinkRequestValidator()
    {
        RuleFor(x => x.TeleconsultLink).MaximumLength(500);
    }
}
