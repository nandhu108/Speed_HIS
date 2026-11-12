using FluentValidation;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Nursing;

public class AddNursingAssessmentRequestValidator : AbstractValidator<AddNursingAssessmentRequest>
{
    public AddNursingAssessmentRequestValidator()
    {
        RuleFor(x => x.TemplateCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DataJson).NotEmpty();
    }
}

public class AddNursingNoteRequestValidator : AbstractValidator<AddNursingNoteRequest>
{
    public AddNursingNoteRequestValidator()
    {
        RuleFor(x => x.Shift).IsInEnum();
        RuleFor(x => x.Notes).NotEmpty();
    }
}

public class AddRiskAssessmentRequestValidator : AbstractValidator<AddRiskAssessmentRequest>
{
    public AddRiskAssessmentRequestValidator()
    {
        RuleFor(x => x.AssessmentType).IsInEnum();
        RuleFor(x => x.ItemScores).NotEmpty();
        // Item ranges of the validated scales: Pain 0-10, Morse Fall Scale items 0-30 (total 0-125),
        // Braden Scale items 1-4 (total 6-23).
        RuleFor(x => x.ItemScores).Must(s => s.Values.All(v => v is >= 0 and <= 10) && s.Values.Sum() <= 10)
            .When(x => x.AssessmentType == Domain.Enums.RiskAssessmentType.Pain).WithMessage("Pain scores must be 0-10.");
        RuleFor(x => x.ItemScores).Must(s => s.Values.All(v => v is >= 0 and <= 30) && s.Values.Sum() <= 125)
            .When(x => x.AssessmentType == Domain.Enums.RiskAssessmentType.FallRisk).WithMessage("Morse Fall Scale items must be 0-30 (total at most 125).");
        RuleFor(x => x.ItemScores).Must(s => s.Values.All(v => v is >= 1 and <= 4))
            .When(x => x.AssessmentType == Domain.Enums.RiskAssessmentType.BedSore).WithMessage("Braden Scale items must be 1-4.");
    }
}

public class AddCarePlanRequestValidator : AbstractValidator<AddCarePlanRequest>
{
    public AddCarePlanRequestValidator()
    {
        RuleFor(x => x.ProblemStatement).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Goal).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Interventions).NotEmpty().MaximumLength(2000);
    }
}

public class AddTransferNoteRequestValidator : AbstractValidator<AddTransferNoteRequest>
{
    public AddTransferNoteRequestValidator()
    {
        RuleFor(x => x.HandoverNotes).NotEmpty().MaximumLength(2000);
    }
}

public class MarActionRequestValidator : AbstractValidator<MarActionRequest>
{
    public MarActionRequestValidator()
    {
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Reason).NotEmpty().When(x => x.Action is MarStatus.Skipped or MarStatus.Refused)
            .WithMessage("A reason is required to skip or refuse a dose.");
        RuleFor(x => x.BatchId).Null().When(x => x.Action != MarStatus.Administered)
            .WithMessage("A stock batch can only be linked when administering a dose.");
        RuleFor(x => x.Quantity).NotNull().GreaterThan(0).When(x => x.BatchId is not null)
            .WithMessage("Quantity is required and must be greater than zero when a batch is selected.");
    }
}

public class PatientHandoverNoteRequestValidator : AbstractValidator<PatientHandoverNoteRequest>
{
    public PatientHandoverNoteRequestValidator()
    {
        RuleFor(x => x.AdmissionId).GreaterThan(0);
        RuleFor(x => x.Situation).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Background).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Assessment).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Recommendation).NotEmpty().MaximumLength(1000);
    }
}

public class CreateShiftHandoverRequestValidator : AbstractValidator<CreateShiftHandoverRequest>
{
    public CreateShiftHandoverRequestValidator()
    {
        RuleFor(x => x.WardId).GreaterThan(0);
        RuleFor(x => x.Shift).IsInEnum();
        RuleForEach(x => x.PatientNotes).SetValidator(new PatientHandoverNoteRequestValidator());
    }
}

public class ReportIncidentRequestValidator : AbstractValidator<ReportIncidentRequest>
{
    public ReportIncidentRequestValidator()
    {
        RuleFor(x => x.IncidentType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Severity).IsInEnum();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
    }
}

public class ReviewIncidentRequestValidator : AbstractValidator<ReviewIncidentRequest>
{
    public ReviewIncidentRequestValidator()
    {
        RuleFor(x => x.ReviewNotes).NotEmpty().MaximumLength(2000);
    }
}

public class RecordIncidentRcaRequestValidator : AbstractValidator<RecordIncidentRcaRequest>
{
    public RecordIncidentRcaRequestValidator()
    {
        RuleFor(x => x.RootCauseAnalysis).NotEmpty().MaximumLength(4000);
    }
}
