using FluentValidation;

namespace Prixo.Application.Features.Icu;

public class SaveCriticalAlertRuleRequestValidator : AbstractValidator<SaveCriticalAlertRuleRequest>
{
    public SaveCriticalAlertRuleRequestValidator()
    {
        RuleFor(x => x.Description).MaximumLength(300);
    }
}

public class AddVentilatorObservationRequestValidator : AbstractValidator<AddVentilatorObservationRequest>
{
    public AddVentilatorObservationRequestValidator()
    {
        RuleFor(x => x.FiO2Percent).InclusiveBetween(21, 100);
        RuleFor(x => x.Peep).InclusiveBetween(0, 30);
    }
}

public class StartInfusionRequestValidator : AbstractValidator<StartInfusionRequest>
{
    public StartInfusionRequestValidator()
    {
        RuleFor(x => x).Must(x => x.DrugId.HasValue || !string.IsNullOrWhiteSpace(x.DrugNameOverride))
            .WithMessage("Specify a catalog drug or a fluid name.");
    }
}

public class AddInfusionRateRequestValidator : AbstractValidator<AddInfusionRateRequest>
{
    public AddInfusionRateRequestValidator()
    {
        RuleFor(x => x.Rate).GreaterThan(0);
        RuleFor(x => x.RateUnit).NotEmpty().MaximumLength(30);
    }
}

public class RecordSeverityRequestValidator : AbstractValidator<RecordSeverityRequest>
{
    public RecordSeverityRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class AcknowledgeAlertRequestValidator : AbstractValidator<AcknowledgeAlertRequest>
{
    public AcknowledgeAlertRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
