using FluentValidation;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Insurance;

public class RaisePreAuthRequestValidator : AbstractValidator<RaisePreAuthRequest>
{
    public RaisePreAuthRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.PolicyId).GreaterThan(0);
        RuleFor(x => x.EstimatedAmount).GreaterThan(0);
        RuleFor(x => x.Diagnosis).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.OpVisitId).NotNull().When(x => x.TreatmentType == PreAuthTreatmentType.Op)
            .WithMessage("Select the OP visit this pre-auth is for.");
        RuleFor(x => x.AdmissionId).NotNull().When(x => x.TreatmentType == PreAuthTreatmentType.Ip)
            .WithMessage("Select the admission this pre-auth is for.");
    }
}

public class DecidePreAuthRequestValidator : AbstractValidator<DecidePreAuthRequest>
{
    public DecidePreAuthRequestValidator()
    {
        RuleFor(x => x.ApprovedAmount).NotNull().GreaterThan(0).When(x => x.Approve);
        RuleFor(x => x.RejectionReason).NotEmpty().MaximumLength(500).When(x => !x.Approve);
    }
}

public class RequestEnhancementRequestValidator : AbstractValidator<RequestEnhancementRequest>
{
    public RequestEnhancementRequestValidator()
    {
        RuleFor(x => x.RequestedAmount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class DecideEnhancementRequestValidator : AbstractValidator<DecideEnhancementRequest>
{
    public DecideEnhancementRequestValidator()
    {
        RuleFor(x => x.RejectionReason).NotEmpty().MaximumLength(500).When(x => !x.Approve);
    }
}

public class SubmitClaimRequestValidator : AbstractValidator<SubmitClaimRequest>
{
    public SubmitClaimRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.PolicyId).GreaterThan(0);
        RuleFor(x => x.BillId).GreaterThan(0);
        RuleFor(x => x.ClaimAmount).GreaterThan(0);
    }
}

public class UpdateClaimStatusRequestValidator : AbstractValidator<UpdateClaimStatusRequest>
{
    public UpdateClaimStatusRequestValidator()
    {
        RuleFor(x => x.ApprovedAmount).NotNull().GreaterThan(0)
            .When(x => x.Status is ClaimStatus.Approved or ClaimStatus.PartiallyApproved)
            .WithMessage("Provide the approved amount.");
        RuleFor(x => x.DenialReasonId).NotNull().When(x => x.Status == ClaimStatus.Denied)
            .WithMessage("Select a denial reason.");
    }
}

public class RecordClaimSettlementRequestValidator : AbstractValidator<RecordClaimSettlementRequest>
{
    public RecordClaimSettlementRequestValidator()
    {
        RuleFor(x => x.SettledAmount).GreaterThan(0);
        RuleFor(x => x.PaymentModeId).GreaterThan(0);
        RuleFor(x => x.DeductionAmount).GreaterThanOrEqualTo(0).When(x => x.DeductionAmount is not null);
    }
}

public class AddClaimDocumentRequestValidator : AbstractValidator<AddClaimDocumentRequest>
{
    public AddClaimDocumentRequestValidator()
    {
        RuleFor(x => x.DocumentTypeId).GreaterThan(0);
    }
}
