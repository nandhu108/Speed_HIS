using FluentAssertions;
using Prixo.Application.Features.Pharmacy;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Pharmacy;

public class SaveStoreRequestValidatorTests
{
    private readonly SaveStoreRequestValidator _validator = new();

    [Fact]
    public void EmptyCode_Fails() =>
        _validator.Validate(new SaveStoreRequest("", "Main Store", StoreType.Pharmacy, true, true)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new SaveStoreRequest("MAIN", "Main Store", StoreType.Pharmacy, true, true)).IsValid.Should().BeTrue();
}

public class CreateGrnItemRequestValidatorTests
{
    private readonly CreateGrnItemRequestValidator _validator = new();

    [Fact]
    public void ZeroMrp_Fails() =>
        _validator.Validate(new CreateGrnItemRequest(1, null, "B001", new DateOnly(2027, 1, 1), 0, 5, 10, 0, 12))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ZeroReceivedQuantity_Fails() =>
        _validator.Validate(new CreateGrnItemRequest(1, null, "B001", new DateOnly(2027, 1, 1), 10, 5, 0, 0, 12))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new CreateGrnItemRequest(1, null, "B001", new DateOnly(2027, 1, 1), 10, 6, 100, 10, 12))
            .IsValid.Should().BeTrue();

    [Fact]
    public void BothDrugAndItem_Fails() =>
        _validator.Validate(new CreateGrnItemRequest(1, 2, "B001", new DateOnly(2027, 1, 1), 10, 6, 100, 10, 12))
            .IsValid.Should().BeFalse();

    [Fact]
    public void NeitherDrugNorItem_Fails() =>
        _validator.Validate(new CreateGrnItemRequest(null, null, "B001", new DateOnly(2027, 1, 1), 10, 6, 100, 10, 12))
            .IsValid.Should().BeFalse();

    [Fact]
    public void GeneralItem_NoExpiryOrMrp_Passes() =>
        _validator.Validate(new CreateGrnItemRequest(null, 2, "B001", null, null, 6, 100, 10, 12))
            .IsValid.Should().BeTrue();
}

public class CreateIndentRequestValidatorTests
{
    private readonly CreateIndentRequestValidator _validator = new();

    [Fact]
    public void NoItems_Fails() =>
        _validator.Validate(new CreateIndentRequest(1, 1, null, [])).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new CreateIndentRequest(1, 1, null, [new CreateIndentItemRequest(1, null, 5)]);
        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}

public class DecideIndentRequestValidatorTests
{
    private readonly DecideIndentRequestValidator _validator = new();

    [Fact]
    public void Reject_WithoutReason_Fails() =>
        _validator.Validate(new DecideIndentRequest(false, null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void Approve_WithoutReason_Passes() =>
        _validator.Validate(new DecideIndentRequest(true, null, null)).IsValid.Should().BeTrue();
}

public class SaleLineRequestValidatorTests
{
    private readonly SaleLineRequestValidator _validator = new();

    [Fact]
    public void ZeroQuantity_Fails() =>
        _validator.Validate(new SaleLineRequest(1, 0, 0)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void DiscountOutOfRange_Fails(int discount) =>
        _validator.Validate(new SaleLineRequest(1, 5, discount)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new SaleLineRequest(1, 5, 10)).IsValid.Should().BeTrue();
}

public class ModifyPayTypeRequestValidatorTests
{
    private readonly ModifyPayTypeRequestValidator _validator = new();

    [Fact]
    public void NullPaymentModeId_Passes() =>
        _validator.Validate(new ModifyPayTypeRequest(null)).IsValid.Should().BeTrue();

    [Fact]
    public void ZeroPaymentModeId_Fails() =>
        _validator.Validate(new ModifyPayTypeRequest(0)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidPaymentModeId_Passes() =>
        _validator.Validate(new ModifyPayTypeRequest(3)).IsValid.Should().BeTrue();
}

public class DecideStockAdjustmentRequestValidatorTests
{
    private readonly DecideStockAdjustmentRequestValidator _validator = new();

    [Fact]
    public void Reject_WithoutReason_Fails() =>
        _validator.Validate(new DecideStockAdjustmentRequest(false, null)).IsValid.Should().BeFalse();

    [Fact]
    public void Reject_WithReason_Passes() =>
        _validator.Validate(new DecideStockAdjustmentRequest(false, "Miscount")).IsValid.Should().BeTrue();

    [Fact]
    public void Approve_WithoutReason_Passes() =>
        _validator.Validate(new DecideStockAdjustmentRequest(true, null)).IsValid.Should().BeTrue();
}

public class CreateStockAdjustmentRequestValidatorTests
{
    private readonly CreateStockAdjustmentRequestValidator _validator = new();

    [Fact]
    public void ZeroQuantityChange_Fails()
    {
        var request = new CreateStockAdjustmentRequest(1, "Damaged", [new CreateStockAdjustmentItemRequest(1, 0)]);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new CreateStockAdjustmentRequest(1, "Damaged", [new CreateStockAdjustmentItemRequest(1, -5)]);
        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}

public class CreateStockTransferRequestValidatorTests
{
    private readonly CreateStockTransferRequestValidator _validator = new();

    [Fact]
    public void NoItems_Fails() =>
        _validator.Validate(new CreateStockTransferRequest(1, 1, 2, [])).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new CreateStockTransferRequest(1, 1, 2, [new CreateStockTransferItemRequest(1, 10)]);
        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}
