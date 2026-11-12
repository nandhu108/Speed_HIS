using FluentValidation;

namespace Prixo.Application.Features.Quality;

public class SaveIndicatorRequestValidator : AbstractValidator<SaveIndicatorRequest>
{
    public SaveIndicatorRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30).Matches("^[A-Z0-9_-]+$")
            .WithMessage("Code must be uppercase letters, digits, '-' or '_'.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NumeratorDefinition).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DenominatorDefinition).NotEmpty().MaximumLength(500);
        RuleFor(x => x.AutoComputeSource).IsInEnum().When(x => x.AutoComputeSource is not null);
    }
}

public class RecordIndicatorEntryRequestValidator : AbstractValidator<RecordIndicatorEntryRequest>
{
    public RecordIndicatorEntryRequestValidator()
    {
        RuleFor(x => x.Denominator).GreaterThan(0);
        RuleFor(x => x.Numerator).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}

public class SaveAuditTemplateRequestValidator : AbstractValidator<SaveAuditTemplateRequest>
{
    public SaveAuditTemplateRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Question).NotEmpty().MaximumLength(500);
            item.RuleFor(i => i.MaxScore).GreaterThan(0);
        });
    }
}

public class ScheduleAuditRequestValidator : AbstractValidator<ScheduleAuditRequest>
{
    public ScheduleAuditRequestValidator()
    {
        RuleFor(x => x.TemplateId).GreaterThan(0);
    }
}

public class ScoreAuditRequestValidator : AbstractValidator<ScoreAuditRequest>
{
    public ScoreAuditRequestValidator()
    {
        RuleFor(x => x.Responses).NotEmpty();
        RuleForEach(x => x.Responses).ChildRules(response =>
        {
            response.RuleFor(r => r.TemplateItemId).GreaterThan(0);
            response.RuleFor(r => r.Score).GreaterThanOrEqualTo(0);
        });
    }
}

public class RaiseNonConformanceRequestValidator : AbstractValidator<RaiseNonConformanceRequest>
{
    public RaiseNonConformanceRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Severity).IsInEnum();
    }
}

public class SaveRiskRequestValidator : AbstractValidator<SaveRiskRequest>
{
    public SaveRiskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Likelihood).InclusiveBetween(1, 5);
        RuleFor(x => x.Impact).InclusiveBetween(1, 5);
        RuleFor(x => x.OwnerUserId).GreaterThan(0);
    }
}

public class RaiseCapaRequestValidator : AbstractValidator<RaiseCapaRequest>
{
    public RaiseCapaRequestValidator()
    {
        RuleFor(x => x.SourceType).IsInEnum();
        RuleFor(x => x.SourceId).GreaterThan(0);
        RuleFor(x => x.ActionPlan).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.OwnerUserId).GreaterThan(0);
    }
}

public class SaveCommitteeMeetingRequestValidator : AbstractValidator<SaveCommitteeMeetingRequest>
{
    public SaveCommitteeMeetingRequestValidator()
    {
        RuleFor(x => x.CommitteeId).GreaterThan(0);
        RuleFor(x => x.Minutes).NotEmpty().MaximumLength(4000);
        RuleForEach(x => x.ActionItems).ChildRules(item =>
        {
            item.RuleFor(i => i.Description).NotEmpty().MaximumLength(500);
            item.RuleFor(i => i.OwnerUserId).GreaterThan(0);
        });
    }
}

public class UploadEvidenceRequestValidator : AbstractValidator<UploadEvidenceRequest>
{
    public UploadEvidenceRequestValidator()
    {
        RuleFor(x => x.StandardId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}
