using FluentAssertions;
using Prixo.Application.Features.Finance;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Finance;

public class FinanceMathIsBalancedTests
{
    [Fact]
    public void EqualDebitsAndCredits_IsBalanced() =>
        FinanceMath.IsBalanced([(100m, 0m), (0m, 100m)]).Should().BeTrue();

    [Fact]
    public void UnequalDebitsAndCredits_IsNotBalanced() =>
        FinanceMath.IsBalanced([(100m, 0m), (0m, 90m)]).Should().BeFalse();

    [Fact]
    public void SingleLine_IsNotBalanced() =>
        FinanceMath.IsBalanced([(100m, 100m)]).Should().BeFalse();

    [Fact]
    public void ThreeLines_MultiAccountSplit_CanBalance() =>
        FinanceMath.IsBalanced([(100m, 0m), (0m, 60m), (0m, 40m)]).Should().BeTrue();
}

public class FinanceMathRecomputePayableTests
{
    [Fact]
    public void NoPayment_IsOpen()
    {
        var (balance, status) = FinanceMath.RecomputePayable(1000m, 0m);
        balance.Should().Be(1000m);
        status.Should().Be(PayableStatus.Open);
    }

    [Fact]
    public void PartialPayment_IsPartiallyPaid()
    {
        var (balance, status) = FinanceMath.RecomputePayable(1000m, 400m);
        balance.Should().Be(600m);
        status.Should().Be(PayableStatus.PartiallyPaid);
    }

    [Fact]
    public void FullPayment_IsPaid()
    {
        var (balance, status) = FinanceMath.RecomputePayable(1000m, 1000m);
        balance.Should().Be(0m);
        status.Should().Be(PayableStatus.Paid);
    }

    [Fact]
    public void Overpayment_BalanceFloorsAtZero()
    {
        var (balance, status) = FinanceMath.RecomputePayable(1000m, 1200m);
        balance.Should().Be(0m);
        status.Should().Be(PayableStatus.Paid);
    }
}

public class FinanceMathRecomputeReceivableTests
{
    [Fact]
    public void NoCollection_IsOpen()
    {
        var (balance, status) = FinanceMath.RecomputeReceivable(5000m, 0m);
        balance.Should().Be(5000m);
        status.Should().Be(ReceivableStatus.Open);
    }

    [Fact]
    public void PartialCollection_IsPartiallyCollected()
    {
        var (balance, status) = FinanceMath.RecomputeReceivable(5000m, 2000m);
        balance.Should().Be(3000m);
        status.Should().Be(ReceivableStatus.PartiallyCollected);
    }

    [Fact]
    public void FullCollection_IsCollected()
    {
        var (balance, status) = FinanceMath.RecomputeReceivable(5000m, 5000m);
        balance.Should().Be(0m);
        status.Should().Be(ReceivableStatus.Collected);
    }
}

public class FinanceMathAgingBucketTests
{
    [Fact]
    public void WithinThirtyDays_IsFirstBucket() =>
        FinanceMath.AgingBucket(new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 1)).Should().Be("0-30");

    [Fact]
    public void FortyFiveDays_IsSecondBucket() =>
        FinanceMath.AgingBucket(new DateOnly(2026, 7, 20), new DateOnly(2026, 6, 5)).Should().Be("31-60");

    [Fact]
    public void SeventyFiveDays_IsThirdBucket() =>
        FinanceMath.AgingBucket(new DateOnly(2026, 7, 20), new DateOnly(2026, 5, 6)).Should().Be("61-90");

    [Fact]
    public void OverNinetyDays_IsLastBucket() =>
        FinanceMath.AgingBucket(new DateOnly(2026, 7, 20), new DateOnly(2026, 1, 1)).Should().Be("90+");
}
