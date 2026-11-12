using FluentValidation;

namespace Prixo.Application.Features.Ot;

public class SaveTheatreRequestValidator : AbstractValidator<SaveTheatreRequest>
{
    public SaveTheatreRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}

public class SaveEquipmentRequestValidator : AbstractValidator<SaveEquipmentRequest>
{
    public SaveEquipmentRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}

public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.AdmissionId).GreaterThan(0);
        RuleFor(x => x.TheatreId).GreaterThan(0);
        RuleFor(x => x.ProcedureName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.SurgeonId).GreaterThan(0);
        RuleFor(x => x.AnaesthetistId).GreaterThan(0).When(x => x.AnaesthetistId is not null);
        RuleFor(x => x.ScheduledEnd).GreaterThan(x => x.ScheduledStart);
    }
}

public class RescheduleBookingRequestValidator : AbstractValidator<RescheduleBookingRequest>
{
    public RescheduleBookingRequestValidator()
    {
        RuleFor(x => x.ScheduledEnd).GreaterThan(x => x.ScheduledStart);
    }
}

public class UpdateBookingPatientRequestValidator : AbstractValidator<UpdateBookingPatientRequest>
{
    public UpdateBookingPatientRequestValidator()
    {
        RuleFor(x => x.AdmissionId).GreaterThan(0);
    }
}

public class CancelBookingRequestValidator : AbstractValidator<CancelBookingRequest>
{
    public CancelBookingRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class PostponeBookingRequestValidator : AbstractValidator<PostponeBookingRequest>
{
    public PostponeBookingRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class VerifyConsentRequestValidator : AbstractValidator<VerifyConsentRequest>
{
    public VerifyConsentRequestValidator()
    {
        RuleFor(x => x.PatientConsentId).GreaterThan(0);
    }
}

public class AddTeamMemberRequestValidator : AbstractValidator<AddTeamMemberRequest>
{
    public AddTeamMemberRequestValidator()
    {
        RuleFor(x => x).Must(x => x.DoctorId.HasValue || !string.IsNullOrWhiteSpace(x.StaffName))
            .WithMessage("Specify a doctor or a staff name.");
    }
}

public class SavePreAnaesthesiaRequestValidator : AbstractValidator<SavePreAnaesthesiaRequest>
{
    public SavePreAnaesthesiaRequestValidator()
    {
        RuleFor(x => x.AssessedByDoctorId).GreaterThan(0);
        RuleFor(x => x.AsaGrade).NotEmpty().MaximumLength(20);
    }
}

public class SaveIntraOpRequestValidator : AbstractValidator<SaveIntraOpRequest>
{
    public SaveIntraOpRequestValidator()
    {
        RuleFor(x => x.AdministeredByDoctorId).GreaterThan(0);
    }
}

public class SaveOperativeNoteRequestValidator : AbstractValidator<SaveOperativeNoteRequest>
{
    public SaveOperativeNoteRequestValidator()
    {
        RuleFor(x => x.SurgeonId).GreaterThan(0);
        RuleFor(x => x.ProcedurePerformed).NotEmpty().MaximumLength(300);
    }
}

public class RecordImplantUsageRequestValidator : AbstractValidator<RecordImplantUsageRequest>
{
    public RecordImplantUsageRequestValidator()
    {
        RuleFor(x => x.BatchId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.SerialNumber).NotEmpty().WithMessage("Enter the implant serial/lot number (needed for traceability and recalls).").MaximumLength(100);
    }
}

public class RecordConsumableUsageRequestValidator : AbstractValidator<RecordConsumableUsageRequest>
{
    public RecordConsumableUsageRequestValidator()
    {
        RuleFor(x => x.BatchId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
