using FluentValidation;

namespace Prixo.Application.Features.Pharmacy;

/// <summary>Shared "exactly one of DrugId/ItemId" rule (Phase 13) for every request DTO that carries a polymorphic product reference.</summary>
internal static class ProductRefValidation
{
    public static void ExactlyOneProduct<T>(this AbstractValidator<T> validator, Func<T, long?> drugId, Func<T, long?> itemId)
    {
        validator.RuleFor(x => x)
            .Must(x => drugId(x).HasValue ^ itemId(x).HasValue)
            .WithMessage("Specify exactly one of a drug or a general item.");
        validator.RuleFor(x => drugId(x)).GreaterThan(0).When(x => drugId(x).HasValue).WithName("DrugId");
        validator.RuleFor(x => itemId(x)).GreaterThan(0).When(x => itemId(x).HasValue).WithName("ItemId");
    }
}

public class SaveStoreRequestValidator : AbstractValidator<SaveStoreRequest>
{
    public SaveStoreRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}

public class CreatePurchaseRequestItemRequestValidator : AbstractValidator<CreatePurchaseRequestItemRequest>
{
    public CreatePurchaseRequestItemRequestValidator()
    {
        this.ExactlyOneProduct(x => x.DrugId, x => x.ItemId);
        RuleFor(x => x.RequestedQuantity).GreaterThan(0);
    }
}

public class CreatePurchaseRequestRequestValidator : AbstractValidator<CreatePurchaseRequestRequest>
{
    public CreatePurchaseRequestRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new CreatePurchaseRequestItemRequestValidator());
    }
}

public class CreatePurchaseOrderItemRequestValidator : AbstractValidator<CreatePurchaseOrderItemRequest>
{
    public CreatePurchaseOrderItemRequestValidator()
    {
        this.ExactlyOneProduct(x => x.DrugId, x => x.ItemId);
        RuleFor(x => x.OrderedQuantity).GreaterThan(0);
        RuleFor(x => x.Rate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxPercent).InclusiveBetween(0, 100);
    }
}

public class CreatePurchaseOrderRequestValidator : AbstractValidator<CreatePurchaseOrderRequest>
{
    public CreatePurchaseOrderRequestValidator()
    {
        RuleFor(x => x.VendorId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new CreatePurchaseOrderItemRequestValidator());
    }
}

public class CreateGrnItemRequestValidator : AbstractValidator<CreateGrnItemRequest>
{
    public CreateGrnItemRequestValidator()
    {
        this.ExactlyOneProduct(x => x.DrugId, x => x.ItemId);
        RuleFor(x => x.BatchNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ExpiryDate).NotNull().WithMessage("Drugs require an expiry date.").When(x => x.DrugId is not null);
        RuleFor(x => x.Mrp).GreaterThan(0).WithMessage("Drugs require an MRP.").When(x => x.DrugId is not null);
        RuleFor(x => x.Mrp).GreaterThanOrEqualTo(0).When(x => x.Mrp is not null);
        RuleFor(x => x.PurchaseRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReceivedQuantity).GreaterThan(0);
        RuleFor(x => x.FreeQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxPercent).InclusiveBetween(0, 100);
    }
}

public class CreateGrnRequestValidator : AbstractValidator<CreateGrnRequest>
{
    public CreateGrnRequestValidator()
    {
        RuleFor(x => x.VendorId).GreaterThan(0);
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new CreateGrnItemRequestValidator());
    }
}

public class CreatePurchaseReturnItemRequestValidator : AbstractValidator<CreatePurchaseReturnItemRequest>
{
    public CreatePurchaseReturnItemRequestValidator()
    {
        RuleFor(x => x.BatchId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Rate).GreaterThanOrEqualTo(0);
    }
}

public class CreatePurchaseReturnRequestValidator : AbstractValidator<CreatePurchaseReturnRequest>
{
    public CreatePurchaseReturnRequestValidator()
    {
        RuleFor(x => x.VendorId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new CreatePurchaseReturnItemRequestValidator());
    }
}

public class SaleLineRequestValidator : AbstractValidator<SaleLineRequest>
{
    public SaleLineRequestValidator()
    {
        RuleFor(x => x.DrugId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
    }
}

public class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleRequestValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new SaleLineRequestValidator());
    }
}

public class CancelSaleRequestValidator : AbstractValidator<CancelSaleRequest>
{
    public CancelSaleRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class ModifyPayTypeRequestValidator : AbstractValidator<ModifyPayTypeRequest>
{
    public ModifyPayTypeRequestValidator()
    {
        RuleFor(x => x.PaymentModeId).GreaterThan(0).When(x => x.PaymentModeId is not null);
    }
}

public class CreateSalesReturnRequestValidator : AbstractValidator<CreateSalesReturnRequest>
{
    public CreateSalesReturnRequestValidator()
    {
        RuleFor(x => x.PharmacySaleId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.PharmacySaleItemId).GreaterThan(0);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}

public class CreateIssueRequestValidator : AbstractValidator<CreateIssueRequest>
{
    public CreateIssueRequestValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.IndentId).GreaterThan(0).When(x => x.IndentId is not null);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.BatchId).GreaterThan(0);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}

public class CreateIndentItemRequestValidator : AbstractValidator<CreateIndentItemRequest>
{
    public CreateIndentItemRequestValidator()
    {
        this.ExactlyOneProduct(x => x.DrugId, x => x.ItemId);
        RuleFor(x => x.RequestedQuantity).GreaterThan(0);
    }
}

public class CreateIndentRequestValidator : AbstractValidator<CreateIndentRequest>
{
    public CreateIndentRequestValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.DepartmentId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new CreateIndentItemRequestValidator());
    }
}

public class DecideIndentRequestValidator : AbstractValidator<DecideIndentRequest>
{
    public DecideIndentRequestValidator()
    {
        RuleFor(x => x.RejectionReason).NotEmpty().MaximumLength(500)
            .When(x => !x.Approve)
            .WithMessage("Give a reason for rejecting the indent.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.IndentItemId).GreaterThan(0);
            item.RuleFor(i => i.ApprovedQuantity).GreaterThanOrEqualTo(0);
        }).When(x => x.Items is not null);
    }
}

public class CreateIssueReturnRequestValidator : AbstractValidator<CreateIssueReturnRequest>
{
    public CreateIssueReturnRequestValidator()
    {
        RuleFor(x => x.IssueId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Items).NotEmpty();
    }
}

public class CreateStockAdjustmentRequestValidator : AbstractValidator<CreateStockAdjustmentRequest>
{
    public CreateStockAdjustmentRequestValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.BatchId).GreaterThan(0);
            item.RuleFor(i => i.QuantityChange).NotEqual(0);
        });
    }
}

public class DecideStockAdjustmentRequestValidator : AbstractValidator<DecideStockAdjustmentRequest>
{
    public DecideStockAdjustmentRequestValidator()
    {
        RuleFor(x => x.RejectionReason).NotEmpty().MaximumLength(500)
            .When(x => !x.Approve)
            .WithMessage("Give a reason for rejecting the adjustment.");
    }
}

public class CreateStockTransferRequestValidator : AbstractValidator<CreateStockTransferRequest>
{
    public CreateStockTransferRequestValidator()
    {
        RuleFor(x => x.FromStoreId).GreaterThan(0);
        RuleFor(x => x.ToBranchId).GreaterThan(0);
        RuleFor(x => x.ToStoreId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.BatchId).GreaterThan(0);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}

public class CreateStockAuditRequestValidator : AbstractValidator<CreateStockAuditRequest>
{
    public CreateStockAuditRequestValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
    }
}

public class AddAuditCountRequestValidator : AbstractValidator<AddAuditCountRequest>
{
    public AddAuditCountRequestValidator()
    {
        RuleFor(x => x.BatchId).GreaterThan(0);
        RuleFor(x => x.CountedQuantity).GreaterThanOrEqualTo(0);
    }
}
