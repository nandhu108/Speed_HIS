using FluentValidation;

namespace Prixo.Application.Features.Billing;

public class GenerateOpBillRequestValidator : AbstractValidator<GenerateOpBillRequest>
{
    public GenerateOpBillRequestValidator()
    {
        RuleFor(x => x.OpVisitId).GreaterThan(0);
    }
}

public class AddManualBillItemRequestValidator : AbstractValidator<AddManualBillItemRequest>
{
    public AddManualBillItemRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.ServiceId).NotNull()
            .When(x => x.SourceType == Domain.Enums.BillItemSourceType.Service)
            .WithMessage("Select a service.");
        RuleFor(x => x.PackageId).NotNull()
            .When(x => x.SourceType == Domain.Enums.BillItemSourceType.Package)
            .WithMessage("Select a package.");
    }
}

public class UpdateBillItemRequestValidator : AbstractValidator<UpdateBillItemRequest>
{
    public UpdateBillItemRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Rate).GreaterThanOrEqualTo(0);
    }
}

public class ApplyItemDiscountRequestValidator : AbstractValidator<ApplyItemDiscountRequest>
{
    public ApplyItemDiscountRequestValidator()
    {
        RuleFor(x => x.Percent).InclusiveBetween(0, 100);
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Enter the reason for the discount/concession.").MaximumLength(300);
    }
}

public class ApplyBillDiscountRequestValidator : AbstractValidator<ApplyBillDiscountRequest>
{
    public ApplyBillDiscountRequestValidator()
    {
        RuleFor(x => x.Percent).InclusiveBetween(0, 100);
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Enter the reason for the discount/concession.").MaximumLength(300);
    }
}

public class CollectPaymentRequestValidator : AbstractValidator<CollectPaymentRequest>
{
    public CollectPaymentRequestValidator()
    {
        RuleFor(x => x.PaymentModeId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
    }
}

public class CancelBillRequestValidator : AbstractValidator<CancelBillRequest>
{
    public CancelBillRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class RequestRefundRequestValidator : AbstractValidator<RequestRefundRequest>
{
    public RequestRefundRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class DecideRefundRequestValidator : AbstractValidator<DecideRefundRequest>
{
    public DecideRefundRequestValidator()
    {
        RuleFor(x => x.RejectionReason).NotEmpty().MaximumLength(500)
            .When(x => !x.Approve)
            .WithMessage("Give a reason for rejecting the refund.");
    }
}

public class DecideDiscountRequestValidator : AbstractValidator<DecideDiscountRequest>
{
    public DecideDiscountRequestValidator()
    {
        RuleFor(x => x.RejectionReason).NotEmpty().MaximumLength(500)
            .When(x => !x.Approve)
            .WithMessage("Give a reason for rejecting the discount.");
    }
}

public class DrawAdvanceOnBillRequestValidator : AbstractValidator<DrawAdvanceOnBillRequest>
{
    public DrawAdvanceOnBillRequestValidator()
    {
        RuleFor(x => x.AdvanceId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public class AddBillNoteRequestValidator : AbstractValidator<AddBillNoteRequest>
{
    public AddBillNoteRequestValidator()
    {
        RuleFor(x => x.Note).NotEmpty().MaximumLength(1000);
    }
}

public class ParkBillRequestValidator : AbstractValidator<ParkBillRequest>
{
    public ParkBillRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class RequestCreditNoteRequestValidator : AbstractValidator<RequestCreditNoteRequest>
{
    public RequestCreditNoteRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(line =>
        {
            line.RuleFor(l => l.BillItemId).GreaterThan(0);
            line.RuleFor(l => l.Amount).GreaterThan(0);
        });
    }
}

public class DecideCreditNoteRequestValidator : AbstractValidator<DecideCreditNoteRequest>
{
    public DecideCreditNoteRequestValidator()
    {
        RuleFor(x => x.RejectionReason).NotEmpty().MaximumLength(500)
            .When(x => !x.Approve)
            .WithMessage("Give a reason for rejecting the credit note.");
    }
}

public class OpenCashierShiftRequestValidator : AbstractValidator<OpenCashierShiftRequest>
{
    public OpenCashierShiftRequestValidator()
    {
        RuleFor(x => x.OpeningFloat).GreaterThanOrEqualTo(0);
    }
}

public class CloseCashierShiftRequestValidator : AbstractValidator<CloseCashierShiftRequest>
{
    public CloseCashierShiftRequestValidator()
    {
        RuleForEach(x => x.DeclaredAmounts).ChildRules(mode =>
        {
            mode.RuleFor(m => m.PaymentModeId).GreaterThan(0);
            mode.RuleFor(m => m.DeclaredAmount).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class CollectAdvanceRequestValidator : AbstractValidator<CollectAdvanceRequest>
{
    public CollectAdvanceRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentModeId).GreaterThan(0);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(300);
    }
}

public class CloseDayRequestValidator : AbstractValidator<CloseDayRequest>
{
    public CloseDayRequestValidator()
    {
        RuleFor(x => x.DeclaredAmounts).NotEmpty();
        RuleForEach(x => x.DeclaredAmounts).ChildRules(mode =>
        {
            mode.RuleFor(m => m.PaymentModeId).GreaterThan(0);
            mode.RuleFor(m => m.DeclaredAmount).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class SavePaymentModeRequestValidator : AbstractValidator<SavePaymentModeRequest>
{
    public SavePaymentModeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300);
    }
}
