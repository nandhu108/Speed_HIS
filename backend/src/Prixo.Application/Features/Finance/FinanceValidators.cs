using FluentValidation;

namespace Prixo.Application.Features.Finance;

public class SaveAccountRequestValidator : AbstractValidator<SaveAccountRequest>
{
    public SaveAccountRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class PostVoucherLineRequestValidator : AbstractValidator<PostVoucherLineRequest>
{
    public PostVoucherLineRequestValidator()
    {
        RuleFor(x => x.AccountId).GreaterThan(0);
        RuleFor(x => x.DebitAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CreditAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Narration).MaximumLength(300);
    }
}

public class PostVoucherRequestValidator : AbstractValidator<PostVoucherRequest>
{
    public PostVoucherRequestValidator()
    {
        RuleFor(x => x.Narration).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Lines).Must(l => l.Count >= 2).WithMessage("A voucher needs at least two lines.");
        RuleForEach(x => x.Lines).SetValidator(new PostVoucherLineRequestValidator());
    }
}

public class CancelVoucherRequestValidator : AbstractValidator<CancelVoucherRequest>
{
    public CancelVoucherRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class CreatePayableRequestValidator : AbstractValidator<CreatePayableRequest>
{
    public CreatePayableRequestValidator()
    {
        RuleFor(x => x.GrnId).GreaterThan(0);
    }
}

public class RecordPayablePaymentRequestValidator : AbstractValidator<RecordPayablePaymentRequest>
{
    public RecordPayablePaymentRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.CashAccountId).GreaterThan(0);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
    }
}

public class CreateReceivableRequestValidator : AbstractValidator<CreateReceivableRequest>
{
    public CreateReceivableRequestValidator()
    {
        RuleFor(x => x.BillId).GreaterThan(0);
    }
}

public class RecordReceivableCollectionRequestValidator : AbstractValidator<RecordReceivableCollectionRequest>
{
    public RecordReceivableCollectionRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.CashAccountId).GreaterThan(0);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
    }
}
