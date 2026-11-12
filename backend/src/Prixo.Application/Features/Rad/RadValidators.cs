using FluentValidation;

namespace Prixo.Application.Features.Rad;

public class SaveRadiologyTestRequestValidator : AbstractValidator<SaveRadiologyTestRequest>
{
    public SaveRadiologyTestRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ModalityId).GreaterThan(0);
        RuleFor(x => x.BodyPart).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TatHours).GreaterThan(0);
    }
}

public class SaveRadReportTemplateRequestValidator : AbstractValidator<SaveRadReportTemplateRequest>
{
    public SaveRadReportTemplateRequestValidator()
    {
        RuleFor(x => x.RadiologyTestId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.FindingsTemplate).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.ImpressionTemplate).NotEmpty().MaximumLength(2000);
    }
}

public class CreateRadOrderRequestValidator : AbstractValidator<CreateRadOrderRequest>
{
    public CreateRadOrderRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.RadiologyTestIds).NotEmpty();
    }
}

public class CancelRadOrderRequestValidator : AbstractValidator<CancelRadOrderRequest>
{
    public CancelRadOrderRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class ScheduleItemRequestValidator : AbstractValidator<ScheduleItemRequest>
{
    public ScheduleItemRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class CaptureContrastConsentRequestValidator : AbstractValidator<CaptureContrastConsentRequest>
{
    public CaptureContrastConsentRequestValidator()
    {
        RuleFor(x => x.SignedByName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RelationshipToPatient).MaximumLength(100);
    }
}

public class EnterReportRequestValidator : AbstractValidator<EnterReportRequest>
{
    public EnterReportRequestValidator()
    {
        RuleFor(x => x.Findings).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Impression).NotEmpty().MaximumLength(2000);
    }
}
