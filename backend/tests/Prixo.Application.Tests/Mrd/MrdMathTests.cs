using FluentAssertions;
using Prixo.Application.Features.Mrd;

namespace Prixo.Application.Tests.Mrd;

public class MrdMathIsOverdueTests
{
    [Fact]
    public void NoExpectedReturnDate_NotOverdue() =>
        MrdMath.IsOverdue(null, new DateOnly(2026, 7, 20)).Should().BeFalse();

    [Fact]
    public void TodayBeforeDueDate_NotOverdue() =>
        MrdMath.IsOverdue(new DateOnly(2026, 7, 25), new DateOnly(2026, 7, 20)).Should().BeFalse();

    [Fact]
    public void TodayEqualsDueDate_NotOverdue() =>
        MrdMath.IsOverdue(new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 20)).Should().BeFalse();

    [Fact]
    public void TodayAfterDueDate_Overdue() =>
        MrdMath.IsOverdue(new DateOnly(2026, 7, 19), new DateOnly(2026, 7, 20)).Should().BeTrue();
}

public class MrdMathEligibleDateTests
{
    [Fact]
    public void AddsRetentionYearsToFromDate() =>
        MrdMath.EligibleDate(new DateOnly(2020, 3, 15), 10).Should().Be(new DateOnly(2030, 3, 15));

    [Fact]
    public void ZeroRetentionYears_ReturnsSameDate() =>
        MrdMath.EligibleDate(new DateOnly(2026, 1, 1), 0).Should().Be(new DateOnly(2026, 1, 1));
}

public class MrdMathIsAuditCompleteTests
{
    [Fact]
    public void AllFlagsTrue_IsComplete() =>
        MrdMath.IsAuditComplete(true, true, true, true, true).Should().BeTrue();

    [Fact]
    public void OneFlagFalse_IsNotComplete() =>
        MrdMath.IsAuditComplete(true, true, false, true, true).Should().BeFalse();

    [Fact]
    public void AllFlagsFalse_IsNotComplete() =>
        MrdMath.IsAuditComplete(false, false, false, false, false).Should().BeFalse();
}
