using FluentAssertions;
using Prixo.Application.Features.Billing;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Billing;

public class BillMathComputeItemAmountsTests
{
    [Fact]
    public void NoDiscountNoTax_TotalEqualsGross()
    {
        var item = new BillItem { BillId = 1, Description = "Consultation", Quantity = 1, Rate = 500 };
        BillMath.ComputeItemAmounts(item);
        item.TotalAmount.Should().Be(500);
        item.DiscountAmount.Should().Be(0);
        item.TaxAmount.Should().Be(0);
    }

    [Fact]
    public void DiscountAppliedBeforeTax()
    {
        // 1000 gross, 10% discount -> 900 taxable, 18% tax -> 162 tax, total 1062.
        var item = new BillItem { BillId = 1, Description = "Service", Quantity = 1, Rate = 1000, DiscountPercent = 10, TaxPercent = 18 };
        BillMath.ComputeItemAmounts(item);
        item.DiscountAmount.Should().Be(100);
        item.TaxAmount.Should().Be(162);
        item.TotalAmount.Should().Be(1062);
    }

    [Fact]
    public void QuantityMultipliesGross()
    {
        var item = new BillItem { BillId = 1, Description = "Item", Quantity = 3, Rate = 150 };
        BillMath.ComputeItemAmounts(item);
        item.TotalAmount.Should().Be(450);
    }

    [Fact]
    public void RoundsDiscountAndTaxToTwoDecimals()
    {
        var item = new BillItem { BillId = 1, Description = "Item", Quantity = 1, Rate = 333.33m, DiscountPercent = 7.5m, TaxPercent = 12 };
        BillMath.ComputeItemAmounts(item);
        item.DiscountAmount.Should().Be(Math.Round(333.33m * 0.075m, 2));
        item.TaxAmount.Should().Be(Math.Round((333.33m - item.DiscountAmount) * 0.12m, 2));
    }
}

public class BillMathApplyItemDiscountPercentTests
{
    [Fact]
    public void SetsPercentAndRecomputesAmounts()
    {
        var item = new BillItem { BillId = 1, Description = "Service", Quantity = 1, Rate = 200, TaxPercent = 10 };
        BillMath.ApplyItemDiscountPercent(item, 25);
        item.DiscountPercent.Should().Be(25);
        item.DiscountAmount.Should().Be(50);
        item.TaxAmount.Should().Be(15); // 10% of (200-50)
        item.TotalAmount.Should().Be(165);
    }
}

public class BillMathRecomputeBillTotalsTests
{
    private static Bill NewBill() => new() { BillNumber = "BILL-2026-000001", PatientId = 1 };

    [Fact]
    public void SumsItemsIntoSubTotalAndTotal()
    {
        var bill = NewBill();
        bill.Items.Add(Item(rate: 500, qty: 1));
        bill.Items.Add(Item(rate: 300, qty: 2));

        BillMath.RecomputeBillTotals(bill);

        bill.SubTotal.Should().Be(1100);
        bill.TotalAmount.Should().Be(1100);
        bill.BalanceAmount.Should().Be(1100);
    }

    [Fact]
    public void BillLevelDiscount_AppliesOnTopOfItemDiscounts()
    {
        var bill = NewBill();
        var item = Item(rate: 1000, qty: 1);
        BillMath.ApplyItemDiscountPercent(item, 10); // item discount 100, base after = 900
        bill.Items.Add(item);
        bill.BillDiscountPercent = 5; // 5% of 900 = 45

        BillMath.RecomputeBillTotals(bill);

        bill.DiscountAmount.Should().Be(145); // 100 item + 45 bill-level
        bill.TotalAmount.Should().Be(855);
    }

    [Fact]
    public void PaidAmountBelowTotal_StatusIsPartiallyPaid()
    {
        var bill = NewBill();
        bill.Items.Add(Item(rate: 1000, qty: 1));
        bill.PaidAmount = 400;

        BillMath.RecomputeBillTotals(bill);

        bill.BalanceAmount.Should().Be(600);
        bill.Status.Should().Be(BillStatus.PartiallyPaid);
    }

    [Fact]
    public void PaidAmountCoversTotal_StatusIsPaid()
    {
        var bill = NewBill();
        bill.Items.Add(Item(rate: 1000, qty: 1));
        bill.PaidAmount = 1000;

        BillMath.RecomputeBillTotals(bill);

        bill.BalanceAmount.Should().Be(0);
        bill.Status.Should().Be(BillStatus.Paid);
    }

    [Fact]
    public void ZeroPaidAmount_DoesNotTouchStatus()
    {
        var bill = NewBill();
        bill.Status = BillStatus.Cancelled;
        bill.Items.Add(Item(rate: 1000, qty: 1));

        BillMath.RecomputeBillTotals(bill);

        bill.Status.Should().Be(BillStatus.Cancelled);
    }

    [Fact]
    public void BalanceNeverGoesNegative()
    {
        var bill = NewBill();
        bill.Items.Add(Item(rate: 100, qty: 1));
        bill.PaidAmount = 150; // over-collected via a since-approved refund elsewhere

        BillMath.RecomputeBillTotals(bill);

        bill.BalanceAmount.Should().Be(0);
    }

    private static BillItem Item(decimal rate, decimal qty) =>
        new() { BillId = 1, Description = "Line", Quantity = qty, Rate = rate };
}

public class BillMathRequiresApprovalTests
{
    [Theory]
    [InlineData(10, 10, false)] // exactly at threshold auto-applies
    [InlineData(5, 10, false)]
    [InlineData(10.01, 10, true)]
    [InlineData(50, 0, true)] // no discount type (threshold 0) always requires approval unless percent is 0
    [InlineData(0, 0, false)]
    public void MatchesThresholdSemantics(double percent, double threshold, bool expected) =>
        BillMath.RequiresApproval((decimal)percent, (decimal)threshold).Should().Be(expected);
}

public class BillMathResolveTariffRateTests
{
    private static readonly DateOnly Today = new(2026, 7, 13);

    [Fact]
    public void NoCandidates_FallsBackToDefaultRate() =>
        BillMath.ResolveTariffRate([], patientCategoryId: null, Today, fallbackRate: 250).Should().Be(250);

    [Fact]
    public void CategorySpecific_PreferredOverCategoryAgnostic()
    {
        TariffCandidate[] candidates =
        [
            new(PatientCategoryId: null, EffectiveFrom: new DateOnly(2026, 1, 1), EffectiveTo: null, Rate: 500),
            new(PatientCategoryId: 7, EffectiveFrom: new DateOnly(2026, 1, 1), EffectiveTo: null, Rate: 350),
        ];
        BillMath.ResolveTariffRate(candidates, patientCategoryId: 7, Today, fallbackRate: 999).Should().Be(350);
    }

    [Fact]
    public void CategoryMismatch_FallsBackToCategoryAgnosticRow()
    {
        TariffCandidate[] candidates =
        [
            new(PatientCategoryId: null, EffectiveFrom: new DateOnly(2026, 1, 1), EffectiveTo: null, Rate: 500),
            new(PatientCategoryId: 7, EffectiveFrom: new DateOnly(2026, 1, 1), EffectiveTo: null, Rate: 350),
        ];
        BillMath.ResolveTariffRate(candidates, patientCategoryId: 42, Today, fallbackRate: 999).Should().Be(500);
    }

    [Fact]
    public void RoomTypeSpecific_PreferredOverRoomTypeAgnostic()
    {
        TariffCandidate[] candidates =
        [
            new(PatientCategoryId: null, EffectiveFrom: new DateOnly(2026, 1, 1), EffectiveTo: null, Rate: 500),
            new(PatientCategoryId: null, EffectiveFrom: new DateOnly(2026, 1, 1), EffectiveTo: null, Rate: 800, RoomTypeId: 3),
        ];
        BillMath.ResolveTariffRate(candidates, patientCategoryId: null, Today, fallbackRate: 999, roomTypeId: 3).Should().Be(800);
    }

    [Fact]
    public void RoomTypeMismatch_FallsBackToRoomTypeAgnosticRow()
    {
        TariffCandidate[] candidates =
        [
            new(PatientCategoryId: null, EffectiveFrom: new DateOnly(2026, 1, 1), EffectiveTo: null, Rate: 500),
            new(PatientCategoryId: null, EffectiveFrom: new DateOnly(2026, 1, 1), EffectiveTo: null, Rate: 800, RoomTypeId: 3),
        ];
        BillMath.ResolveTariffRate(candidates, patientCategoryId: null, Today, fallbackRate: 999, roomTypeId: 9).Should().Be(500);
    }

    [Fact]
    public void BothCategoryAndRoomTypeSpecific_PreferredOverEitherAlone()
    {
        TariffCandidate[] candidates =
        [
            new(PatientCategoryId: 7, EffectiveFrom: new DateOnly(2026, 1, 1), EffectiveTo: null, Rate: 600),
            new(PatientCategoryId: null, EffectiveFrom: new DateOnly(2026, 1, 1), EffectiveTo: null, Rate: 800, RoomTypeId: 3),
            new(PatientCategoryId: 7, EffectiveFrom: new DateOnly(2026, 1, 1), EffectiveTo: null, Rate: 950, RoomTypeId: 3),
        ];
        BillMath.ResolveTariffRate(candidates, patientCategoryId: 7, Today, fallbackRate: 999, roomTypeId: 3).Should().Be(950);
    }

    [Fact]
    public void MostRecentEffectiveFrom_Wins()
    {
        TariffCandidate[] candidates =
        [
            new(PatientCategoryId: null, EffectiveFrom: new DateOnly(2025, 1, 1), EffectiveTo: null, Rate: 400),
            new(PatientCategoryId: null, EffectiveFrom: new DateOnly(2026, 6, 1), EffectiveTo: null, Rate: 450),
        ];
        BillMath.ResolveTariffRate(candidates, patientCategoryId: null, Today, fallbackRate: 999).Should().Be(450);
    }

    [Fact]
    public void ExpiredTariff_IsIgnored()
    {
        TariffCandidate[] candidates =
        [
            new(PatientCategoryId: null, EffectiveFrom: new DateOnly(2025, 1, 1), EffectiveTo: new DateOnly(2026, 1, 1), Rate: 400),
        ];
        BillMath.ResolveTariffRate(candidates, patientCategoryId: null, Today, fallbackRate: 999).Should().Be(999);
    }

    [Fact]
    public void FutureTariff_IsIgnored()
    {
        TariffCandidate[] candidates =
        [
            new(PatientCategoryId: null, EffectiveFrom: new DateOnly(2027, 1, 1), EffectiveTo: null, Rate: 400),
        ];
        BillMath.ResolveTariffRate(candidates, patientCategoryId: null, Today, fallbackRate: 999).Should().Be(999);
    }
}

public class BillMathPlanAdvanceSweepTests
{
    [Fact]
    public void NoAdvances_PlansNothing() =>
        BillMath.PlanAdvanceSweep([], billBalance: 500).Should().BeEmpty();

    [Fact]
    public void BillFullyPaid_PlansNothing() =>
        BillMath.PlanAdvanceSweep([new AdvanceBalance(1, 500)], billBalance: 0).Should().BeEmpty();

    [Fact]
    public void SingleAdvanceCoversBalance_DrawsExactBalance()
    {
        var draws = BillMath.PlanAdvanceSweep([new AdvanceBalance(1, 1000)], billBalance: 400);
        draws.Should().ContainSingle();
        draws[0].AdvanceId.Should().Be(1);
        draws[0].Amount.Should().Be(400);
    }

    [Fact]
    public void SingleAdvanceSmallerThanBalance_DrawsWholeAdvance()
    {
        var draws = BillMath.PlanAdvanceSweep([new AdvanceBalance(1, 150)], billBalance: 400);
        draws.Should().ContainSingle();
        draws[0].Amount.Should().Be(150);
    }

    [Fact]
    public void MultipleAdvances_DrawnInGivenFifoOrderUntilCovered()
    {
        // Caller supplies FIFO order (oldest ReceivedAt first); sweep must respect that order.
        AdvanceBalance[] advances = [new(1, 100), new(2, 100), new(3, 500)];
        var draws = BillMath.PlanAdvanceSweep(advances, billBalance: 250);

        draws.Should().HaveCount(3);
        draws[0].Should().Be(new AdvanceDraw(1, 100));
        draws[1].Should().Be(new AdvanceDraw(2, 100));
        draws[2].Should().Be(new AdvanceDraw(3, 50)); // only the remaining 50 needed, not the full 500
    }

    [Fact]
    public void ZeroBalanceAdvance_IsSkippedWithoutStoppingTheSweep()
    {
        AdvanceBalance[] advances = [new(1, 0), new(2, 300)];
        var draws = BillMath.PlanAdvanceSweep(advances, billBalance: 200);

        draws.Should().ContainSingle();
        draws[0].Should().Be(new AdvanceDraw(2, 200));
    }

    [Fact]
    public void TotalAdvanceBalanceLessThanBillBalance_DrawsEverythingAvailable()
    {
        AdvanceBalance[] advances = [new(1, 100), new(2, 50)];
        var draws = BillMath.PlanAdvanceSweep(advances, billBalance: 1000);

        draws.Should().HaveCount(2);
        draws.Sum(d => d.Amount).Should().Be(150);
    }
}
