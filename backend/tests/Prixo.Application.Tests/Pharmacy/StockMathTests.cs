using FluentAssertions;
using Prixo.Application.Features.Pharmacy;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Pharmacy;

public class StockMathIsInboundTests
{
    [Theory]
    [InlineData(StockMovementType.PurchaseIn, true)]
    [InlineData(StockMovementType.SaleReturnIn, true)]
    [InlineData(StockMovementType.IssueReturnIn, true)]
    [InlineData(StockMovementType.AdjustmentIn, true)]
    [InlineData(StockMovementType.TransferIn, true)]
    [InlineData(StockMovementType.PurchaseReturnOut, false)]
    [InlineData(StockMovementType.SaleOut, false)]
    [InlineData(StockMovementType.IssueOut, false)]
    [InlineData(StockMovementType.AdjustmentOut, false)]
    [InlineData(StockMovementType.TransferOut, false)]
    public void ClassifiesEveryMovementType(StockMovementType type, bool expected) =>
        StockMath.IsInbound(type).Should().Be(expected);
}

public class StockMathApplyMovementTests
{
    [Fact]
    public void Inbound_IncreasesBalance() =>
        StockMath.ApplyMovement(100, StockMovementType.PurchaseIn, 20).Should().Be(120);

    [Fact]
    public void Outbound_DecreasesBalance() =>
        StockMath.ApplyMovement(100, StockMovementType.SaleOut, 20).Should().Be(80);
}

public class StockMathAllocateFefoTests
{
    [Fact]
    public void NoCandidates_AllocatesNothing()
    {
        var (allocations, remaining) = StockMath.AllocateFefo([], 10);
        allocations.Should().BeEmpty();
        remaining.Should().Be(10);
    }

    [Fact]
    public void SingleBatch_CoversFully()
    {
        FefoBatchCandidate[] candidates = [new(1, new DateOnly(2027, 1, 1), 50)];
        var (allocations, remaining) = StockMath.AllocateFefo(candidates, 30);
        allocations.Should().ContainSingle(a => a.BatchId == 1 && a.Quantity == 30);
        remaining.Should().Be(0);
    }

    [Fact]
    public void MultipleBatches_EarliestExpiryConsumedFirst()
    {
        FefoBatchCandidate[] candidates =
        [
            new(1, new DateOnly(2028, 1, 1), 50),
            new(2, new DateOnly(2027, 1, 1), 20),
        ];
        var (allocations, remaining) = StockMath.AllocateFefo(candidates, 30);
        allocations.Should().HaveCount(2);
        allocations[0].BatchId.Should().Be(2); // earlier expiry first
        allocations[0].Quantity.Should().Be(20);
        allocations[1].BatchId.Should().Be(1);
        allocations[1].Quantity.Should().Be(10);
        remaining.Should().Be(0);
    }

    [Fact]
    public void InsufficientStock_ReturnsShortfall()
    {
        FefoBatchCandidate[] candidates = [new(1, new DateOnly(2027, 1, 1), 10)];
        var (allocations, remaining) = StockMath.AllocateFefo(candidates, 30);
        allocations.Should().ContainSingle(a => a.Quantity == 10);
        remaining.Should().Be(20);
    }

    [Fact]
    public void ZeroAvailableBatch_IsSkipped()
    {
        FefoBatchCandidate[] candidates = [new(1, new DateOnly(2027, 1, 1), 0), new(2, new DateOnly(2028, 1, 1), 15)];
        var (allocations, remaining) = StockMath.AllocateFefo(candidates, 10);
        allocations.Should().ContainSingle(a => a.BatchId == 2);
        remaining.Should().Be(0);
    }
}

public class StockMathComputeLineAmountsTests
{
    [Fact]
    public void NoDiscountNoTax_TotalEqualsGross()
    {
        var (discount, tax, total) = StockMath.ComputeLineAmounts(rate: 10, quantity: 5, discountPercent: 0, taxPercent: 0);
        discount.Should().Be(0);
        tax.Should().Be(0);
        total.Should().Be(50);
    }

    [Fact]
    public void DiscountAppliedBeforeTax()
    {
        // 20 units * 10 rate = 200 gross, 5% discount = 10, taxable 190, 12% tax = 22.80, total 212.80.
        var (discount, tax, total) = StockMath.ComputeLineAmounts(rate: 10, quantity: 20, discountPercent: 5, taxPercent: 12);
        discount.Should().Be(10);
        tax.Should().Be(22.80m);
        total.Should().Be(212.80m);
    }
}
