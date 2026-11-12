using FluentValidation;

namespace Prixo.Application.Features.Lab;

public class SaveReferenceRangeRequestValidator : AbstractValidator<SaveReferenceRangeRequest>
{
    public SaveReferenceRangeRequestValidator()
    {
        RuleFor(x => x.MinAgeYears).GreaterThanOrEqualTo(0).When(x => x.MinAgeYears.HasValue);
        RuleFor(x => x.MaxAgeYears).GreaterThanOrEqualTo(0).When(x => x.MaxAgeYears.HasValue);
        RuleFor(x => x)
            .Must(x => x.MinAgeYears is null || x.MaxAgeYears is null || x.MinAgeYears <= x.MaxAgeYears)
            .WithMessage("Minimum age cannot exceed maximum age.");
        RuleFor(x => x)
            .Must(x => x.NormalLow is null || x.NormalHigh is null || x.NormalLow <= x.NormalHigh)
            .WithMessage("Normal low cannot exceed normal high.");
        RuleFor(x => x.TextNormalValue).MaximumLength(200);
    }
}

public class SaveLabTestParameterRequestValidator : AbstractValidator<SaveLabTestParameterRequest>
{
    public SaveLabTestParameterRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Unit).MaximumLength(30);
        RuleForEach(x => x.ReferenceRanges).SetValidator(new SaveReferenceRangeRequestValidator());
    }
}

public class SaveLabTestRequestValidator : AbstractValidator<SaveLabTestRequest>
{
    public SaveLabTestRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.SampleTypeId).GreaterThan(0);
        RuleFor(x => x.Method).MaximumLength(100);
        RuleFor(x => x.TatHours).GreaterThan(0);
        RuleFor(x => x.Parameters).NotEmpty().WithMessage("A test needs at least one parameter.");
        RuleForEach(x => x.Parameters).SetValidator(new SaveLabTestParameterRequestValidator());
    }
}

public class SaveTestProfileRequestValidator : AbstractValidator<SaveTestProfileRequest>
{
    public SaveTestProfileRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).When(x => x.Price.HasValue);
        RuleFor(x => x.LabTestIds).NotEmpty().WithMessage("A profile needs at least one test.");
    }
}

public class CreateLabOrderItemRequestValidator : AbstractValidator<CreateLabOrderItemRequest>
{
    public CreateLabOrderItemRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.LabTestId is not null ^ x.TestProfileId is not null)
            .WithMessage("Each line must be either a single test or a profile, not both or neither.");
    }
}

public class CreateLabOrderRequestValidator : AbstractValidator<CreateLabOrderRequest>
{
    public CreateLabOrderRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.ClinicalNotes).MaximumLength(1000);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Select at least one test or profile.");
        RuleForEach(x => x.Items).SetValidator(new CreateLabOrderItemRequestValidator());
    }
}

public class CancelLabOrderRequestValidator : AbstractValidator<CancelLabOrderRequest>
{
    public CancelLabOrderRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class CollectSampleRequestValidator : AbstractValidator<CollectSampleRequest>
{
    public CollectSampleRequestValidator()
    {
        RuleFor(x => x.LabOrderId).GreaterThan(0);
        RuleFor(x => x.SampleTypeId).GreaterThan(0);
        RuleFor(x => x.LabOrderItemIds).NotEmpty();
    }
}

public class RejectSampleRequestValidator : AbstractValidator<RejectSampleRequest>
{
    public RejectSampleRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class EnterResultValueRequestValidator : AbstractValidator<EnterResultValueRequest>
{
    public EnterResultValueRequestValidator()
    {
        RuleFor(x => x.LabTestParameterId).GreaterThan(0);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(200);
    }
}

public class EnterResultRequestValidator : AbstractValidator<EnterResultRequest>
{
    public EnterResultRequestValidator()
    {
        RuleFor(x => x.Values).NotEmpty();
        RuleForEach(x => x.Values).SetValidator(new EnterResultValueRequestValidator());
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
