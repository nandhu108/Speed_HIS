using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Quality;

/// <summary>
/// KPI Monitoring dashboard (indicator trend vs target) and Compliance Dashboard (plan §20) —
/// read-only aggregation, same posture as IcuDashboardService/BiomedReportService: no new
/// persisted table, just queries over what the rest of the module already writes.
/// </summary>
public class QualityDashboardService(IAppDbContext db, TimeProvider timeProvider)
{
    public async Task<KpiDashboardDto> GetKpiDashboardAsync(DateOnly fromPeriod, DateOnly toPeriod, CancellationToken ct = default)
    {
        var entries = await db.QualityIndicatorEntries.AsNoTracking()
            .Where(e => e.PeriodMonth >= fromPeriod && e.PeriodMonth <= toPeriod)
            .Include(e => e.Indicator)
            .OrderBy(e => e.Indicator!.Name).ThenBy(e => e.PeriodMonth)
            .ToListAsync(ct);

        var points = entries.Select(e => new IndicatorTrendPointDto(
            e.IndicatorId, e.Indicator!.Name, e.Indicator.Unit, e.Indicator.TargetValue, e.PeriodMonth,
            QualityMath.ComputedValue(e.Numerator, e.Denominator))).ToList();
        return new KpiDashboardDto(points);
    }

    public async Task<ComplianceDashboardDto> GetComplianceDashboardAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var yearStart = new DateTimeOffset(new DateOnly(today.Year, 1, 1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var openNonConformances = await db.QualityNonConformances.AsNoTracking()
            .CountAsync(n => n.Status != NonConformanceStatus.Closed, ct);
        var openCapas = await db.QualityCapas.AsNoTracking().CountAsync(c => c.Status != CapaStatus.Closed, ct);
        var overdueCapas = await db.QualityCapas.AsNoTracking()
            .CountAsync(c => c.Status != CapaStatus.Closed && c.DueDate < today, ct);
        var openRisks = await db.QualityRisks.AsNoTracking().CountAsync(r => r.Status != RiskStatus.Closed, ct);
        var sentinelIncidents = await db.Incidents.AsNoTracking()
            .CountAsync(i => i.IsSentinelEvent && i.IncidentDate >= yearStart, ct);
        var evidenceCount = await db.QualityEvidence.AsNoTracking().CountAsync(ct);

        var completedAudits = await db.QualityAudits.AsNoTracking()
            .Where(a => a.Status == QualityAuditStatus.Completed)
            .Include(a => a.Responses).Include(a => a.Template).ThenInclude(t => t!.Items)
            .ToListAsync(ct);
        var scores = completedAudits
            .Select(a => QualityMath.AuditScorePercent(
                a.Responses.Select(r => (r.Score, a.Template!.Items.First(i => i.Id == r.TemplateItemId).MaxScore)).ToList()))
            .Where(s => s is not null)
            .Select(s => s!.Value)
            .ToList();
        var averageScore = scores.Count == 0 ? (decimal?)null : Math.Round(scores.Average(), 2);

        var evidenceByChapter = (await db.QualityEvidence.AsNoTracking().Include(e => e.Standard).ToListAsync(ct))
            .GroupBy(e => QualityMath.ChapterOf(e.Standard!.Code))
            .Select(g => new ChapterEvidenceCountDto(g.Key, g.Count()))
            .OrderBy(g => g.Chapter)
            .ToList();

        return new ComplianceDashboardDto(
            openNonConformances, openCapas, overdueCapas, openRisks, sentinelIncidents,
            averageScore, evidenceCount, evidenceByChapter);
    }
}
