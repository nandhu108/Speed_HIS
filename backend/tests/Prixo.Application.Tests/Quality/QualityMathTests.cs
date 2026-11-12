using FluentAssertions;
using Prixo.Application.Features.Quality;

namespace Prixo.Application.Tests.Quality;

public class QualityMathComputedValueTests
{
    [Fact]
    public void ZeroDenominator_ReturnsNull() =>
        QualityMath.ComputedValue(5, 0).Should().BeNull();

    [Fact]
    public void OccupancyStyleRatio_ReturnsFraction() =>
        QualityMath.ComputedValue(40, 50).Should().Be(0.8m);

    [Fact]
    public void RawCountStyleDenominatorOne_ReturnsNumeratorUnscaled() =>
        QualityMath.ComputedValue(5, 1).Should().Be(5m);
}

public class QualityMathRiskScoreTests
{
    [Fact]
    public void LikelihoodTimesImpact_ReturnsProduct() =>
        QualityMath.RiskScore(3, 4).Should().Be(12);

    [Fact]
    public void MinimumBoth_ReturnsOne() =>
        QualityMath.RiskScore(1, 1).Should().Be(1);

    [Fact]
    public void MaximumBoth_ReturnsTwentyFive() =>
        QualityMath.RiskScore(5, 5).Should().Be(25);
}

public class QualityMathAuditScorePercentTests
{
    [Fact]
    public void NoResponses_ReturnsNull() =>
        QualityMath.AuditScorePercent([]).Should().BeNull();

    [Fact]
    public void ZeroMaxScore_ReturnsNull() =>
        QualityMath.AuditScorePercent([(0, 0)]).Should().BeNull();

    [Fact]
    public void FullMarks_ReturnsHundred() =>
        QualityMath.AuditScorePercent([(5, 5), (5, 5)]).Should().Be(100m);

    [Fact]
    public void HalfMarks_ReturnsFifty() =>
        QualityMath.AuditScorePercent([(5, 10)]).Should().Be(50m);
}

public class QualityMathChapterOfTests
{
    [Fact]
    public void CodeWithUnderscore_ReturnsPrefixBeforeUnderscore() =>
        QualityMath.ChapterOf("AAC_1").Should().Be("AAC");

    [Fact]
    public void CodeWithoutUnderscore_ReturnsWholeCode() =>
        QualityMath.ChapterOf("AAC").Should().Be("AAC");
}
