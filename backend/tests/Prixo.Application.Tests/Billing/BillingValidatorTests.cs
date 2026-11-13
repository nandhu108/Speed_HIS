using FluentAssertions;
using Prixo.Application.Features.Billing;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Billing;

public class GenerateOpBillRequestValidatorTests
{
    private readonly GenerateOpBillRequestValidator _validator = new();

    [Fact]
    public void ZeroVisitId_Fails() =>
        _validator.Validate(new GenerateOpBillRequest(0)).IsValid.Should().BeFalse();

    [Fact]
    public void PositiveVisitId_Passes() =>
        _validator.Validate(new GenerateOpBillRequest(1)).IsValid.Should().BeTrue();
}

public class AddManualBillItemRequestValidatorTests
{
    private readonly AddManualBillItemRequestValidator _validator = new();

    [Fact]
    public void Service_WithoutServiceId_Fails() =>
        _validator.Validate(new AddManualBillItemRequest(BillItemSourceType.Service, null, null, 1)).IsValid.Should().BeFalse();

    [Fact]
    public void Service_WithServiceId_Passes() =>
        _validator.Validate(new AddManualBillItemRequest(BillItemSourceType.Service, 5, null, 1)).IsValid.Should().BeTrue();

    [Fact]
    public void Package_WithoutPackageId_Fails() =>
        _validator.Validate(new AddManualBillItemRequest(BillItemSourceType.Package, null, null, 1)).IsValid.Should().BeFalse();

    [Fact]
    public void ZeroQuantity_Fails() =>
        _validator.Validate(new AddManualBillItemRequest(BillItemSourceType.Service, 5, null, 0)).IsValid.Should().BeFalse();
}

public class ApplyItemDiscountRequestValidatorTests
{
    private readonly ApplyItemDiscountRequestValidator _validator = new();

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void OutOfRangePercent_Fails(int percent) =>
        _validator.Validate(new ApplyItemDiscountRequest(null, percent, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ZeroPercent_NoDiscountType_Passes() =>
        _validator.Validate(new ApplyItemDiscountRequest(null, 0, "Staff concession")).IsValid.Should().BeTrue();

    [Fact]
    public void HundredPercent_Passes() =>
        _validator.Validate(new ApplyItemDiscountRequest(1, 100, "Goodwill")).IsValid.Should().BeTrue();
}

public class CollectPaymentRequestValidatorTests
{
    private readonly CollectPaymentRequestValidator _validator = new();

    [Fact]
    public void ZeroAmount_Fails() =>
        _validator.Validate(new CollectPaymentRequest(1, 0, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ZeroPaymentModeId_Fails() =>
        _validator.Validate(new CollectPaymentRequest(0, 100, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new CollectPaymentRequest(1, 100, "REF123")).IsValid.Should().BeTrue();
}

public class DecideRefundRequestValidatorTests
{
    private readonly DecideRefundRequestValidator _validator = new();

    [Fact]
    public void Reject_WithoutReason_Fails() =>
        _validator.Validate(new DecideRefundRequest(false, null)).IsValid.Should().BeFalse();

    [Fact]
    public void Reject_WithReason_Passes() =>
        _validator.Validate(new DecideRefundRequest(false, "Duplicate payment")).IsValid.Should().BeTrue();

    [Fact]
    public void Approve_WithoutReason_Passes() =>
        _validator.Validate(new DecideRefundRequest(true, null)).IsValid.Should().BeTrue();
}

public class DecideDiscountRequestValidatorTests
{
    private readonly DecideDiscountRequestValidator _validator = new();

    [Fact]
    public void Reject_WithoutReason_Fails() =>
        _validator.Validate(new DecideDiscountRequest(false, null)).IsValid.Should().BeFalse();

    [Fact]
    public void Approve_WithoutReason_Passes() =>
        _validator.Validate(new DecideDiscountRequest(true, null)).IsValid.Should().BeTrue();
}

public class DrawAdvanceOnBillRequestValidatorTests
{
    private readonly DrawAdvanceOnBillRequestValidator _validator = new();

    [Fact]
    public void ZeroAdvanceId_Fails() =>
        _validator.Validate(new DrawAdvanceOnBillRequest(0, 100)).IsValid.Should().BeFalse();

    [Fact]
    public void ZeroAmount_Fails() =>
        _validator.Validate(new DrawAdvanceOnBillRequest(1, 0)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new DrawAdvanceOnBillRequest(1, 100)).IsValid.Should().BeTrue();
}

public class AddBillNoteRequestValidatorTests
{
    private readonly AddBillNoteRequestValidator _validator = new();

    [Fact]
    public void EmptyNote_Fails() =>
        _validator.Validate(new AddBillNoteRequest("")).IsValid.Should().BeFalse();

    [Fact]
    public void NonEmptyNote_Passes() =>
        _validator.Validate(new AddBillNoteRequest("Called patient re: balance")).IsValid.Should().BeTrue();
}

public class ParkBillRequestValidatorTests
{
    private readonly ParkBillRequestValidator _validator = new();

    [Fact]
    public void EmptyReason_Fails() =>
        _validator.Validate(new ParkBillRequest("")).IsValid.Should().BeFalse();

    [Fact]
    public void WithReason_Passes() =>
        _validator.Validate(new ParkBillRequest("Awaiting insurance approval")).IsValid.Should().BeTrue();
}

public class RequestCreditNoteRequestValidatorTests
{
    private readonly RequestCreditNoteRequestValidator _validator = new();

    [Fact]
    public void EmptyItems_Fails() =>
        _validator.Validate(new RequestCreditNoteRequest("Billing error", [])).IsValid.Should().BeFalse();

    [Fact]
    public void ZeroLineAmount_Fails() =>
        _validator.Validate(new RequestCreditNoteRequest("Billing error", [new CreditNoteItemRequest(1, 0)])).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RequestCreditNoteRequest("Billing error", [new CreditNoteItemRequest(1, 100)])).IsValid.Should().BeTrue();
}

public class DecideCreditNoteRequestValidatorTests
{
    private readonly DecideCreditNoteRequestValidator _validator = new();

    [Fact]
    public void Reject_WithoutReason_Fails() =>
        _validator.Validate(new DecideCreditNoteRequest(false, null)).IsValid.Should().BeFalse();

    [Fact]
    public void Approve_WithoutReason_Passes() =>
        _validator.Validate(new DecideCreditNoteRequest(true, null)).IsValid.Should().BeTrue();
}

public class OpenCashierShiftRequestValidatorTests
{
    private readonly OpenCashierShiftRequestValidator _validator = new();

    [Fact]
    public void NegativeOpeningFloat_Fails() =>
        _validator.Validate(new OpenCashierShiftRequest(-1)).IsValid.Should().BeFalse();

    [Fact]
    public void ZeroOpeningFloat_Passes() =>
        _validator.Validate(new OpenCashierShiftRequest(0)).IsValid.Should().BeTrue();
}

public class CloseCashierShiftRequestValidatorTests
{
    private readonly CloseCashierShiftRequestValidator _validator = new();

    [Fact]
    public void NegativeDeclaredAmount_Fails()
    {
        var request = new CloseCashierShiftRequest([new DeclaredModeAmountRequest(1, -10)], null);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new CloseCashierShiftRequest([new DeclaredModeAmountRequest(1, 500)], "Tallied");
        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}

public class CreateBillEstimateRequestValidatorTests
{
    private readonly CreateBillEstimateRequestValidator _validator = new();

    [Fact]
    public void EmptyItems_Fails() =>
        _validator.Validate(new CreateBillEstimateRequest(1, null, [], null)).IsValid.Should().BeFalse();

    [Fact]
    public void ServiceLine_WithoutServiceId_Fails()
    {
        var request = new CreateBillEstimateRequest(1, null, [new CreateEstimateItemRequest(BillItemSourceType.Service, null, null, 1)], null);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new CreateBillEstimateRequest(1, null, [new CreateEstimateItemRequest(BillItemSourceType.Package, null, 1, 1)], "Pre-admission estimate");
        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}

public class CloseDayRequestValidatorTests
{
    private readonly CloseDayRequestValidator _validator = new();

    [Fact]
    public void EmptyDeclaredAmounts_Fails() =>
        _validator.Validate(new CloseDayRequest(new DateOnly(2026, 7, 13), [], null)).IsValid.Should().BeFalse();

    [Fact]
    public void NegativeDeclaredAmount_Fails()
    {
        var request = new CloseDayRequest(new DateOnly(2026, 7, 13), [new DeclaredModeAmountRequest(1, -10)], null);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new CloseDayRequest(new DateOnly(2026, 7, 13), [new DeclaredModeAmountRequest(1, 500)], "Cash tallied");
        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}
