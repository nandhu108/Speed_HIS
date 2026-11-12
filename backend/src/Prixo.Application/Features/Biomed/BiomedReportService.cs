using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Biomed;

/// <summary>Equipment Downtime Report and PM Compliance report (plan §19) — read-only aggregates, same posture as MrdReportService/FinanceReportService. AMC expiry is served by AmcContractService's renewalDueOnly filter, not duplicated here.</summary>
public class BiomedReportService(IAppDbContext db)
{
    public async Task<IReadOnlyList<DowntimeReportRowDto>> GetDowntimeReportAsync(BiomedReportDateRange range, CancellationToken ct = default)
    {
        var complaints = db.BreakdownComplaints.AsNoTracking().Where(c => c.ResolvedAt != null).Include(c => c.Equipment).AsQueryable();
        if (range.FromDate is { } from) complaints = complaints.Where(c => c.ReportedAt >= new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (range.ToDate is { } to) complaints = complaints.Where(c => c.ReportedAt < new DateTimeOffset(to.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(1));

        // Grouped client-side (same posture as GetPmComplianceReportAsync below) — the equivalent
        // server-side GroupBy/Select combination over the Equipment navigation + nullable Sum
        // doesn't translate to SQL under EF Core 10 (2026-09-17 fix; this endpoint 500'd outright).
        var rows = await complaints
            .Select(c => new { c.EquipmentId, EquipmentName = c.Equipment!.Name, c.DowntimeMinutes })
            .ToListAsync(ct);

        return rows.GroupBy(r => new { r.EquipmentId, r.EquipmentName })
            .Select(g => new DowntimeReportRowDto(g.Key.EquipmentId, g.Key.EquipmentName, g.Count(), g.Sum(r => r.DowntimeMinutes ?? 0)))
            .OrderByDescending(r => r.TotalDowntimeMinutes)
            .ToList();
    }

    public async Task<IReadOnlyList<PmComplianceRowDto>> GetPmComplianceReportAsync(BiomedReportDateRange range, CancellationToken ct = default)
    {
        var workOrders = db.WorkOrders.AsNoTracking()
            .Where(w => w.WorkOrderType == WorkOrderType.PreventiveMaintenance && w.Status == WorkOrderStatus.Completed)
            .Include(w => w.Equipment).AsQueryable();
        if (range.FromDate is { } from) workOrders = workOrders.Where(w => w.ScheduledDate >= from);
        if (range.ToDate is { } to) workOrders = workOrders.Where(w => w.ScheduledDate <= to);

        var rows = await workOrders
            .Select(w => new { w.EquipmentId, EquipmentName = w.Equipment!.Name, w.ScheduledDate, CompletedDate = w.CompletedAt!.Value })
            .ToListAsync(ct);

        return rows.GroupBy(r => new { r.EquipmentId, r.EquipmentName })
            .Select(g =>
            {
                var total = g.Count();
                var onTime = g.Count(r => DateOnly.FromDateTime(r.CompletedDate.UtcDateTime) <= r.ScheduledDate);
                return new PmComplianceRowDto(g.Key.EquipmentId, g.Key.EquipmentName, total, onTime, BiomedMath.PmCompliancePercent(total, onTime));
            })
            .OrderBy(r => r.EquipmentName)
            .ToList();
    }
}
