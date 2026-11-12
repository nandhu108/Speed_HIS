using FluentAssertions;
using Prixo.Application.Features.Masters.Loyalty;

namespace Prixo.Application.Tests.Masters;

public class SaveLoyaltyCardTypeRequestValidatorTests
{
    private readonly SaveLoyaltyCardTypeRequestValidator _validator = new();

    [Fact]
    public void EmptyCode_Fails() =>
        _validator.Validate(new SaveLoyaltyCardTypeRequest("", "Gold", null, 4, null)).IsValid.Should().BeFalse();

    [Fact]
    public void LowercaseCode_Fails() =>
        _validator.Validate(new SaveLoyaltyCardTypeRequest("gold", "Gold", null, 4, null)).IsValid.Should().BeFalse();

    [Fact]
    public void NegativeMemberLimit_Fails() =>
        _validator.Validate(new SaveLoyaltyCardTypeRequest("GOLD", "Gold", null, -1, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new SaveLoyaltyCardTypeRequest("GOLD", "Gold", "Gold tier", 4, 1)).IsValid.Should().BeTrue();
}

public class SaveLoyaltyCardPriceRequestValidatorTests
{
    private readonly SaveLoyaltyCardPriceRequestValidator _validator = new();

    [Fact]
    public void NoItems_Fails() =>
        _validator.Validate(new SaveLoyaltyCardPriceRequest(1, new DateOnly(2026, 1, 1), null, [])).IsValid.Should().BeFalse();

    [Fact]
    public void EffectiveToBeforeFrom_Fails() =>
        _validator.Validate(new SaveLoyaltyCardPriceRequest(
            1, new DateOnly(2026, 6, 1), new DateOnly(2026, 1, 1),
            [new SaveLoyaltyCardPriceItemRequest(12, 1000)])).IsValid.Should().BeFalse();

    [Fact]
    public void NegativeAmount_Fails() =>
        _validator.Validate(new SaveLoyaltyCardPriceRequest(
            1, new DateOnly(2026, 1, 1), null,
            [new SaveLoyaltyCardPriceItemRequest(12, -1)])).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new SaveLoyaltyCardPriceRequest(
            1, new DateOnly(2026, 1, 1), null,
            [new SaveLoyaltyCardPriceItemRequest(12, 1000)])).IsValid.Should().BeTrue();
}
