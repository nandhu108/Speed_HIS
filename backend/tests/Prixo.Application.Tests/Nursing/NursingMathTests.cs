using FluentAssertions;
using Prixo.Application.Features.Nursing;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Nursing;

public class NursingMathGenerateDueTimesTests
{
    private static readonly DateOnly StartDate = new(2027, 1, 1);
    private static readonly DateTimeOffset OrderedAt = new(2027, 1, 1, 7, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Od_GeneratesOneDueTime()
    {
        var times = NursingMath.GenerateDueTimes("OD", StartDate, null, StartDate, OrderedAt);
        times.Should().HaveCount(1);
        times[0].Should().Be(new DateTimeOffset(2027, 1, 1, 8, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Bd_GeneratesTwoDueTimesInOrder()
    {
        var times = NursingMath.GenerateDueTimes("BD", StartDate, null, StartDate, OrderedAt);
        times.Should().Equal(
            new DateTimeOffset(2027, 1, 1, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2027, 1, 1, 20, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Tid_GeneratesThreeDueTimes()
    {
        NursingMath.GenerateDueTimes("TID", StartDate, null, StartDate, OrderedAt).Should().HaveCount(3);
    }

    [Fact]
    public void CaseInsensitiveFrequencyCode_Matches()
    {
        NursingMath.GenerateDueTimes("bd", StartDate, null, StartDate, OrderedAt).Should().HaveCount(2);
    }

    [Fact]
    public void Sos_GeneratesNoScheduledDoses()
    {
        NursingMath.GenerateDueTimes("SOS", StartDate, null, StartDate, OrderedAt).Should().BeEmpty();
    }

    [Fact]
    public void UnrecognizedFrequency_GeneratesNothing()
    {
        NursingMath.GenerateDueTimes("QOD", StartDate, null, StartDate, OrderedAt).Should().BeEmpty();
    }

    [Fact]
    public void Stat_OnOrderDate_GeneratesSingleImmediateDose()
    {
        var times = NursingMath.GenerateDueTimes("STAT", StartDate, null, StartDate, OrderedAt);
        times.Should().Equal(OrderedAt);
    }

    [Fact]
    public void Stat_OnLaterDate_GeneratesNothing()
    {
        NursingMath.GenerateDueTimes("STAT", StartDate, null, StartDate.AddDays(1), OrderedAt).Should().BeEmpty();
    }

    [Fact]
    public void ForDateBeforeStartDate_GeneratesNothing()
    {
        NursingMath.GenerateDueTimes("OD", StartDate, null, StartDate.AddDays(-1), OrderedAt).Should().BeEmpty();
    }

    [Fact]
    public void ForDateAfterEndDate_GeneratesNothing()
    {
        NursingMath.GenerateDueTimes("OD", StartDate, StartDate, StartDate.AddDays(1), OrderedAt).Should().BeEmpty();
    }

    [Fact]
    public void ForDateOnEndDate_StillGenerates()
    {
        NursingMath.GenerateDueTimes("OD", StartDate, StartDate, StartDate, OrderedAt).Should().HaveCount(1);
    }
}

public class NursingMathScoreTests
{
    [Fact]
    public void SumScore_AddsAllItemValues() => NursingMath.SumScore([10, 0, 15, 20]).Should().Be(45);

    [Theory]
    [InlineData(0, "None")]
    [InlineData(3, "Mild")]
    [InlineData(6, "Moderate")]
    [InlineData(10, "Severe")]
    public void Pain_BandsCorrectly(int score, string expected) =>
        NursingMath.ResolveRiskLevel(RiskAssessmentType.Pain, score).Should().Be(expected);

    [Theory]
    [InlineData(0, "Low")]
    [InlineData(24, "Low")]
    [InlineData(25, "Moderate")]
    [InlineData(44, "Moderate")]
    [InlineData(45, "High")]
    [InlineData(125, "High")]
    public void FallRisk_MorseBandsCorrectly(int score, string expected) =>
        NursingMath.ResolveRiskLevel(RiskAssessmentType.FallRisk, score).Should().Be(expected);

    [Theory]
    [InlineData(6, "Severe")]
    [InlineData(9, "Severe")]
    [InlineData(10, "High")]
    [InlineData(12, "High")]
    [InlineData(13, "Moderate")]
    [InlineData(14, "Moderate")]
    [InlineData(15, "Mild")]
    [InlineData(18, "Mild")]
    [InlineData(19, "None")]
    [InlineData(23, "None")]
    public void BedSore_BradenBandsCorrectly(int score, string expected) =>
        NursingMath.ResolveRiskLevel(RiskAssessmentType.BedSore, score).Should().Be(expected);
}
