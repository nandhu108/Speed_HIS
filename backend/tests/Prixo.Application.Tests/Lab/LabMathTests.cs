using FluentAssertions;
using Prixo.Application.Features.Lab;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Lab;

public class LabMathCalculateAgeYearsTests
{
    [Fact]
    public void BirthdayAlreadyPassedThisYear_CountsFullYears() =>
        LabMath.CalculateAgeYears(new DateOnly(1990, 1, 1), null, new DateOnly(2026, 7, 13)).Should().Be(36);

    [Fact]
    public void BirthdayNotYetReachedThisYear_SubtractsOne() =>
        LabMath.CalculateAgeYears(new DateOnly(1990, 12, 31), null, new DateOnly(2026, 7, 13)).Should().Be(35);

    [Fact]
    public void BirthdayIsToday_CountsFullYear() =>
        LabMath.CalculateAgeYears(new DateOnly(1990, 7, 13), null, new DateOnly(2026, 7, 13)).Should().Be(36);

    [Fact]
    public void NoDateOfBirth_FallsBackToEstimatedAge() =>
        LabMath.CalculateAgeYears(null, 40, new DateOnly(2026, 7, 13)).Should().Be(40);

    [Fact]
    public void NeitherProvided_ReturnsNull() =>
        LabMath.CalculateAgeYears(null, null, new DateOnly(2026, 7, 13)).Should().BeNull();
}

public class LabMathResolveRangeTests
{
    [Fact]
    public void NoCandidates_ReturnsNull() =>
        LabMath.ResolveRange([], Gender.Male, 30).Should().BeNull();

    [Fact]
    public void SexSpecific_PreferredOverSexAgnostic()
    {
        ReferenceRangeCandidate[] candidates =
        [
            new(1, Sex: null, MinAgeYears: null, MaxAgeYears: null, NormalLow: 10, NormalHigh: 20, CriticalLow: null, CriticalHigh: null, TextNormalValue: null),
            new(2, Sex: Gender.Male, MinAgeYears: null, MaxAgeYears: null, NormalLow: 13, NormalHigh: 17, CriticalLow: null, CriticalHigh: null, TextNormalValue: null),
        ];
        LabMath.ResolveRange(candidates, Gender.Male, 30)!.Id.Should().Be(2);
    }

    [Fact]
    public void SexMismatch_FallsBackToSexAgnosticRow()
    {
        ReferenceRangeCandidate[] candidates =
        [
            new(1, Sex: null, MinAgeYears: null, MaxAgeYears: null, NormalLow: 10, NormalHigh: 20, CriticalLow: null, CriticalHigh: null, TextNormalValue: null),
            new(2, Sex: Gender.Female, MinAgeYears: null, MaxAgeYears: null, NormalLow: 13, NormalHigh: 17, CriticalLow: null, CriticalHigh: null, TextNormalValue: null),
        ];
        LabMath.ResolveRange(candidates, Gender.Male, 30)!.Id.Should().Be(1);
    }

    [Fact]
    public void AgeOutsideBounds_ExcludesRow()
    {
        ReferenceRangeCandidate[] candidates =
        [
            new(1, Sex: null, MinAgeYears: 18, MaxAgeYears: 60, NormalLow: 10, NormalHigh: 20, CriticalLow: null, CriticalHigh: null, TextNormalValue: null),
        ];
        LabMath.ResolveRange(candidates, Gender.Male, 10).Should().BeNull();
    }

    [Fact]
    public void AgeBoundedRow_PreferredOverUnbounded()
    {
        ReferenceRangeCandidate[] candidates =
        [
            new(1, Sex: null, MinAgeYears: null, MaxAgeYears: null, NormalLow: 10, NormalHigh: 20, CriticalLow: null, CriticalHigh: null, TextNormalValue: null),
            new(2, Sex: null, MinAgeYears: 0, MaxAgeYears: 12, NormalLow: 11, NormalHigh: 15, CriticalLow: null, CriticalHigh: null, TextNormalValue: null),
        ];
        LabMath.ResolveRange(candidates, Gender.Male, 5)!.Id.Should().Be(2);
    }

    [Fact]
    public void UnknownAge_StillMatchesUnboundedRow()
    {
        ReferenceRangeCandidate[] candidates =
        [
            new(1, Sex: null, MinAgeYears: null, MaxAgeYears: null, NormalLow: 10, NormalHigh: 20, CriticalLow: null, CriticalHigh: null, TextNormalValue: null),
        ];
        LabMath.ResolveRange(candidates, Gender.Male, null)!.Id.Should().Be(1);
    }
}

public class LabMathResolveFlagTests
{
    private static readonly ReferenceRangeCandidate Range = new(
        1, Sex: null, MinAgeYears: null, MaxAgeYears: null, NormalLow: 12, NormalHigh: 16, CriticalLow: 5, CriticalHigh: 20, TextNormalValue: null);

    [Fact]
    public void NoRange_AlwaysNormal() =>
        LabMath.ResolveFlag(999, null).Should().Be(ResultFlag.Normal);

    [Fact]
    public void WithinNormalRange_Normal() =>
        LabMath.ResolveFlag(14, Range).Should().Be(ResultFlag.Normal);

    [Fact]
    public void BelowNormalButAboveCritical_Low() =>
        LabMath.ResolveFlag(8, Range).Should().Be(ResultFlag.Low);

    [Fact]
    public void AboveNormalButBelowCritical_High() =>
        LabMath.ResolveFlag(18, Range).Should().Be(ResultFlag.High);

    [Fact]
    public void BelowCriticalLow_Critical() =>
        LabMath.ResolveFlag(4, Range).Should().Be(ResultFlag.Critical);

    [Fact]
    public void AboveCriticalHigh_Critical() =>
        LabMath.ResolveFlag(21, Range).Should().Be(ResultFlag.Critical);

    [Fact]
    public void ExactlyAtNormalBoundary_Normal() =>
        LabMath.ResolveFlag(12, Range).Should().Be(ResultFlag.Normal);

    [Fact]
    public void ExactlyAtCriticalLowBoundary_NotCriticalYet() =>
        // 5 is not < 5, so the critical check doesn't trip — it falls through to the normal-low check instead.
        LabMath.ResolveFlag(5, Range).Should().Be(ResultFlag.Low);
}

public class LabMathRecomputeOrderStatusTests
{
    [Fact]
    public void AllOrdered_StaysOrdered() =>
        LabMath.RecomputeOrderStatus([LabOrderItemStatus.Ordered, LabOrderItemStatus.Ordered]).Should().Be(LabOrderStatus.Ordered);

    [Fact]
    public void SomeProgressed_InProgress() =>
        LabMath.RecomputeOrderStatus([LabOrderItemStatus.Ordered, LabOrderItemStatus.Collected]).Should().Be(LabOrderStatus.InProgress);

    [Fact]
    public void AllApproved_Completed() =>
        LabMath.RecomputeOrderStatus([LabOrderItemStatus.Approved, LabOrderItemStatus.Approved]).Should().Be(LabOrderStatus.Completed);

    [Fact]
    public void ApprovedPlusCancelled_StillCompleted() =>
        LabMath.RecomputeOrderStatus([LabOrderItemStatus.Approved, LabOrderItemStatus.Cancelled]).Should().Be(LabOrderStatus.Completed);

    [Fact]
    public void AllCancelled_Cancelled() =>
        LabMath.RecomputeOrderStatus([LabOrderItemStatus.Cancelled, LabOrderItemStatus.Cancelled]).Should().Be(LabOrderStatus.Cancelled);

    [Fact]
    public void EmptyList_Cancelled() =>
        LabMath.RecomputeOrderStatus([]).Should().Be(LabOrderStatus.Cancelled);

    [Fact]
    public void CancelledPlusInProgress_InProgress() =>
        LabMath.RecomputeOrderStatus([LabOrderItemStatus.Cancelled, LabOrderItemStatus.Received]).Should().Be(LabOrderStatus.InProgress);
}
