using Prixo.Domain.Entities.Reports;

namespace Prixo.Application.Features.Reports;

/// <summary>Shared date-range filtering for the three `reporting` materialized-view query types — used by both DashboardService and ReportExecutionService so the range semantics stay identical between a dashboard widget and the same data reached through the custom report builder.</summary>
internal static class ReportRangeFilters
{
    public static IQueryable<RevenueDailyRow> InRange(IQueryable<RevenueDailyRow> query, ReportDateRange range)
    {
        if (range.FromDate is { } from) query = query.Where(r => r.RevenueDate >= from);
        if (range.ToDate is { } to) query = query.Where(r => r.RevenueDate <= to);
        return query;
    }

    public static IQueryable<DailyCollectionRow> InRange(IQueryable<DailyCollectionRow> query, ReportDateRange range)
    {
        if (range.FromDate is { } from) query = query.Where(r => r.CollectionDate >= from);
        if (range.ToDate is { } to) query = query.Where(r => r.CollectionDate <= to);
        return query;
    }

    public static IQueryable<OpCensusRow> InRange(IQueryable<OpCensusRow> query, ReportDateRange range)
    {
        if (range.FromDate is { } from) query = query.Where(r => r.VisitDate >= from);
        if (range.ToDate is { } to) query = query.Where(r => r.VisitDate <= to);
        return query;
    }
}
