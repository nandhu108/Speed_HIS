using FluentAssertions;
using Prixo.Application.Features.Biomed;

namespace Prixo.Application.Tests.Biomed;

public class BiomedMathIsRenewalDueTests
{
    [Fact]
    public void WellBeforeEndDate_NotDue() =>
        BiomedMath.IsRenewalDue(new DateOnly(2026, 12, 31), 30, new DateOnly(2026, 7, 20)).Should().BeFalse();

    [Fact]
    public void WithinReminderWindow_IsDue() =>
        BiomedMath.IsRenewalDue(new DateOnly(2026, 8, 10), 30, new DateOnly(2026, 7, 20)).Should().BeTrue();

    [Fact]
    public void PastEndDate_IsDue() =>
        BiomedMath.IsRenewalDue(new DateOnly(2026, 7, 1), 30, new DateOnly(2026, 7, 20)).Should().BeTrue();
}

public class BiomedMathIsPmDueTests
{
    [Fact]
    public void FutureDueDate_NotDue() =>
        BiomedMath.IsPmDue(new DateOnly(2026, 8, 1), new DateOnly(2026, 7, 20)).Should().BeFalse();

    [Fact]
    public void TodayIsDueDate_IsDue() =>
        BiomedMath.IsPmDue(new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 20)).Should().BeTrue();

    [Fact]
    public void PastDueDate_IsDue() =>
        BiomedMath.IsPmDue(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 20)).Should().BeTrue();
}

public class BiomedMathDowntimeMinutesTests
{
    [Fact]
    public void OneHourGap_ReturnsSixty()
    {
        var reported = new DateTimeOffset(2026, 7, 20, 8, 0, 0, TimeSpan.Zero);
        var resolved = new DateTimeOffset(2026, 7, 20, 9, 0, 0, TimeSpan.Zero);
        BiomedMath.DowntimeMinutes(reported, resolved).Should().Be(60);
    }

    [Fact]
    public void ZeroGap_ReturnsZero()
    {
        var now = new DateTimeOffset(2026, 7, 20, 8, 0, 0, TimeSpan.Zero);
        BiomedMath.DowntimeMinutes(now, now).Should().Be(0);
    }
}

public class BiomedMathPmCompliancePercentTests
{
    [Fact]
    public void NoCompletedWorkOrders_ReturnsZero() =>
        BiomedMath.PmCompliancePercent(0, 0).Should().Be(0m);

    [Fact]
    public void AllOnTime_ReturnsHundred() =>
        BiomedMath.PmCompliancePercent(10, 10).Should().Be(100m);

    [Fact]
    public void HalfOnTime_ReturnsFifty() =>
        BiomedMath.PmCompliancePercent(10, 5).Should().Be(50m);
}
