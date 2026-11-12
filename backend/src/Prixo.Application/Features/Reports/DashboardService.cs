using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Auth;
using Prixo.Application.Features.Ipd;
using Prixo.Domain.Entities.Reports;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Reports;

/// <summary>
/// Cross-cutting Phase 23 dashboards, reading from the `reporting` schema's materialized
/// views (RevenueDailyRows/DailyCollectionRows/OpCensusRows) rather than raw OLTP joins,
/// plus composing IPD's existing BedBoardService for the IP census widget outright (no
/// parallel occupancy calculation). The Management Dashboard is the one cross-branch-aware
/// read: it self-adapts to whether the caller holds REPORT.CROSSBRANCH.VIEW rather than
/// exposing a separate cross-branch endpoint, since that's the one place plan §23 explicitly
/// calls out "cross-branch with permission" on a single dashboard.
/// </summary>
public class DashboardService(IAppDbContext db, ICurrentUser currentUser, PermissionService permissions, BedBoardService bedBoard)
{
    public async Task<ManagementDashboardDto> GetManagementDashboardAsync(ReportDateRange range, CancellationToken ct = default)
    {
        var crossBranch = await permissions.HasPermissionAsync(currentUser.UserId, PermissionRegistry.ReportCrossBranchView, ct);
        var branchId = currentUser.BranchId;
        if (!crossBranch && branchId is null)
        {
            throw new DomainException("Select a branch first.");
        }

        var revenue = ReportRangeFilters.InRange(db.RevenueDailyRows.AsNoTracking(), range).Where(r => crossBranch || r.BranchId == branchId);
        var collection = ReportRangeFilters.InRange(db.DailyCollectionRows.AsNoTracking(), range).Where(r => crossBranch || r.BranchId == branchId);
        var opCensus = ReportRangeFilters.InRange(db.OpCensusRows.AsNoTracking(), range).Where(r => crossBranch || r.BranchId == branchId);
        var admissions = db.Admissions.AsNoTracking().IgnoreQueryFilters()
            .Where(a => a.Status == AdmissionStatus.Admitted && (crossBranch || a.BranchId == branchId));

        var alertCount = await db.CriticalAlerts.AsNoTracking().IgnoreQueryFilters()
            .Where(a => !a.IsAcknowledged && (crossBranch || a.BranchId == branchId)).CountAsync(ct)
            + await db.IcuVitalsAlerts.AsNoTracking().IgnoreQueryFilters()
            .Where(a => !a.IsAcknowledged && (crossBranch || a.BranchId == branchId)).CountAsync(ct);

        var revenueByBranch = await revenue.GroupBy(r => r.BranchId).Select(g => new { BranchId = g.Key, Amount = g.Sum(r => r.Amount) }).ToListAsync(ct);
        var collectionByBranch = await collection.GroupBy(r => r.BranchId).Select(g => new { BranchId = g.Key, Amount = g.Sum(r => r.Amount) }).ToListAsync(ct);
        var opByBranch = await opCensus.GroupBy(r => r.BranchId).Select(g => new { BranchId = g.Key, Count = g.Sum(r => r.VisitCount) }).ToListAsync(ct);
        var ipByBranch = await admissions.GroupBy(a => a.BranchId).Select(g => new { BranchId = g.Key, Count = g.Count() }).ToListAsync(ct);

        var branchIds = revenueByBranch.Select(b => b.BranchId)
            .Union(collectionByBranch.Select(b => b.BranchId)).Union(opByBranch.Select(b => b.BranchId)).Union(ipByBranch.Select(b => b.BranchId))
            .Distinct().ToList();
        var branchNames = await db.Branches.AsNoTracking().IgnoreQueryFilters()
            .Where(b => branchIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.Name, ct);

        var byBranch = branchIds.Select(id => new BranchSummaryRowDto(
            id, branchNames.GetValueOrDefault(id, $"Branch #{id}"),
            revenueByBranch.FirstOrDefault(b => b.BranchId == id)?.Amount ?? 0,
            collectionByBranch.FirstOrDefault(b => b.BranchId == id)?.Amount ?? 0,
            opByBranch.FirstOrDefault(b => b.BranchId == id)?.Count ?? 0,
            ipByBranch.FirstOrDefault(b => b.BranchId == id)?.Count ?? 0))
            .OrderBy(b => b.BranchName).ToList();

        var occupancy = await bedBoard.GetOccupancyDashboardAsync(ct);

        return new ManagementDashboardDto(
            byBranch.Sum(b => b.Revenue), byBranch.Sum(b => b.Collection), byBranch.Sum(b => b.OpVisitCount), byBranch.Sum(b => b.ActiveInpatients),
            occupancy.OccupancyRatePercent, alertCount, crossBranch, byBranch);
    }

    public async Task<OpDashboardDto> GetOpDashboardAsync(ReportDateRange range, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var rows = await ReportRangeFilters.InRange(db.OpCensusRows.AsNoTracking().Where(r => r.BranchId == branchId), range).ToListAsync(ct);

        var byDoctor = rows.GroupBy(r => new { r.DoctorId, r.DoctorName })
            .Select(g => new DoctorVisitCountRowDto(g.Key.DoctorId, g.Key.DoctorName, g.Sum(r => r.VisitCount)))
            .OrderByDescending(d => d.VisitCount).ToList();
        var dailyTrend = rows.GroupBy(r => r.VisitDate)
            .Select(g => new TrendPointDto(g.Key, g.Sum(r => r.VisitCount)))
            .OrderBy(t => t.Date).ToList();

        // VisitType: New = 1, Review = 2 (Prixo.Domain.Enums.VisitType).
        return new OpDashboardDto(
            rows.Sum(r => r.VisitCount), rows.Where(r => r.VisitType == 1).Sum(r => r.VisitCount), rows.Where(r => r.VisitType == 2).Sum(r => r.VisitCount),
            dailyTrend, byDoctor);
    }

    public async Task<IpDashboardDto> GetIpDashboardAsync(ReportDateRange range, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var occupancy = await bedBoard.GetOccupancyDashboardAsync(ct);

        var admissions = db.Admissions.AsNoTracking().Where(a => a.BranchId == branchId);

        var admissionsInRange = admissions.AsQueryable();
        if (range.FromDate is { } from) admissionsInRange = admissionsInRange.Where(a => a.AdmissionDate >= AtStart(from));
        if (range.ToDate is { } to) admissionsInRange = admissionsInRange.Where(a => a.AdmissionDate < AtStart(to.AddDays(1)));
        var admissionsCount = await admissionsInRange.CountAsync(ct);

        var discharged = admissions.Where(a => a.Status == AdmissionStatus.Discharged && a.DischargedAt != null);
        if (range.FromDate is { } dFrom) discharged = discharged.Where(a => a.DischargedAt >= AtStart(dFrom));
        if (range.ToDate is { } dTo) discharged = discharged.Where(a => a.DischargedAt < AtStart(dTo.AddDays(1)));
        var dischargedList = await discharged.Select(a => new { a.AdmissionDate, a.DischargedAt }).ToListAsync(ct);

        decimal? alos = dischargedList.Count == 0 ? null :
            Math.Round((decimal)dischargedList.Average(a => (a.DischargedAt!.Value - a.AdmissionDate).TotalDays), 1);

        return new IpDashboardDto(occupancy.TotalBeds, occupancy.Occupied, occupancy.Vacant, occupancy.OccupancyRatePercent,
            admissionsCount, dischargedList.Count, alos);
    }

    public async Task<BillingDashboardDto> GetBillingDashboardAsync(ReportDateRange range, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var rows = await ReportRangeFilters.InRange(db.DailyCollectionRows.AsNoTracking().Where(r => r.BranchId == branchId), range).ToListAsync(ct);

        var byMode = rows.GroupBy(r => new { r.PaymentModeId, r.PaymentModeName })
            .Select(g => new PaymentModeAmountRowDto(g.Key.PaymentModeId, g.Key.PaymentModeName, g.Sum(r => r.Amount), g.Sum(r => r.TxnCount)))
            .OrderByDescending(m => m.Amount).ToList();
        var dailyTrend = rows.GroupBy(r => r.CollectionDate)
            .Select(g => new TrendPointDto(g.Key, g.Sum(r => r.Amount)))
            .OrderBy(t => t.Date).ToList();

        var outstanding = await db.Bills.AsNoTracking()
            .Where(b => b.BranchId == branchId && b.Status != BillStatus.Cancelled && b.BalanceAmount > 0)
            .SumAsync(b => b.BalanceAmount, ct);

        return new BillingDashboardDto(rows.Sum(r => r.Amount), byMode, dailyTrend, outstanding);
    }

    public async Task<RevenueDashboardDto> GetRevenueDashboardAsync(ReportDateRange range, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var rows = await ReportRangeFilters.InRange(db.RevenueDailyRows.AsNoTracking().Where(r => r.BranchId == branchId), range).ToListAsync(ct);

        var byDepartment = rows.GroupBy(r => new { r.DepartmentId, DepartmentName = r.DepartmentName ?? "Unassigned" })
            .Select(g => new DepartmentAmountRowDto(g.Key.DepartmentId, g.Key.DepartmentName, g.Sum(r => r.Amount)))
            .OrderByDescending(d => d.Amount).ToList();
        var byDoctor = rows.Where(r => r.DoctorId != null).GroupBy(r => new { r.DoctorId, DoctorName = r.DoctorName ?? "Unknown" })
            .Select(g => new DoctorAmountRowDto(g.Key.DoctorId, g.Key.DoctorName, g.Sum(r => r.Amount)))
            .OrderByDescending(d => d.Amount).ToList();
        var dailyTrend = rows.GroupBy(r => r.RevenueDate)
            .Select(g => new TrendPointDto(g.Key, g.Sum(r => r.Amount)))
            .OrderBy(t => t.Date).ToList();

        return new RevenueDashboardDto(rows.Sum(r => r.Amount), dailyTrend, byDepartment, byDoctor);
    }

    private static DateTimeOffset AtStart(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
