using FluentAssertions;
using Prixo.Application.Features.Finance;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Finance;

public class SaveAccountRequestValidatorTests
{
    private readonly SaveAccountRequestValidator _validator = new();

    [Fact]
    public void EmptyCode_Fails() =>
        _validator.Validate(new SaveAccountRequest("", "Cash", AccountType.Asset, null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new SaveAccountRequest("1000", "Cash", AccountType.Asset, null, null)).IsValid.Should().BeTrue();
}

public class PostVoucherRequestValidatorTests
{
    private readonly PostVoucherRequestValidator _validator = new();

    [Fact]
    public void SingleLine_Fails() =>
        _validator.Validate(new PostVoucherRequest(VoucherType.Journal, new DateOnly(2026, 7, 20), "Narration",
            [new PostVoucherLineRequest(1, 100m, 0m, null)])).IsValid.Should().BeFalse();

    [Fact]
    public void EmptyNarration_Fails() =>
        _validator.Validate(new PostVoucherRequest(VoucherType.Journal, new DateOnly(2026, 7, 20), "",
            [new PostVoucherLineRequest(1, 100m, 0m, null), new PostVoucherLineRequest(2, 0m, 100m, null)])).IsValid.Should().BeFalse();

    [Fact]
    public void NegativeAmount_Fails() =>
        _validator.Validate(new PostVoucherRequest(VoucherType.Journal, new DateOnly(2026, 7, 20), "Narration",
            [new PostVoucherLineRequest(1, -100m, 0m, null), new PostVoucherLineRequest(2, 0m, 100m, null)])).IsValid.Should().BeFalse();

    [Fact]
    public void TwoBalancedLines_Passes() =>
        _validator.Validate(new PostVoucherRequest(VoucherType.Journal, new DateOnly(2026, 7, 20), "Opening balance",
            [new PostVoucherLineRequest(1, 100m, 0m, null), new PostVoucherLineRequest(2, 0m, 100m, null)])).IsValid.Should().BeTrue();
}

public class RecordPayablePaymentRequestValidatorTests
{
    private readonly RecordPayablePaymentRequestValidator _validator = new();

    [Fact]
    public void ZeroAmount_Fails() =>
        _validator.Validate(new RecordPayablePaymentRequest(0m, 1, new DateOnly(2026, 7, 20), null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RecordPayablePaymentRequest(500m, 1, new DateOnly(2026, 7, 20), "REF123")).IsValid.Should().BeTrue();
}
