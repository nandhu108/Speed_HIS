namespace Prixo.Application.Features.Quality;

public record IndicatorTrendPointDto(long IndicatorId, string IndicatorName, string? Unit, decimal? TargetValue, DateOnly PeriodMonth, decimal? ComputedValue);
public record KpiDashboardDto(IReadOnlyList<IndicatorTrendPointDto> Points);

public record ComplianceDashboardDto(
    int OpenNonConformances, int OpenCapas, int OverdueCapas, int OpenRisks, int SentinelIncidentsThisYear,
    decimal? AverageAuditScorePercent, int EvidenceCount, IReadOnlyList<ChapterEvidenceCountDto> EvidenceByChapter);

public record ChapterEvidenceCountDto(string Chapter, int Count);
