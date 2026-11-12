using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Auth;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Reports;

/// <summary>
/// Custom Report Builder v1's generic executor (plan §17: "Generic executor endpoint + typed
/// endpoints for complex reports"). One API surface for every executable report_definitions
/// row; internally it dispatches to a small, safe, typed query per known `reporting` view
/// rather than building raw SQL from user input — the "generic" contract is at the HTTP
/// layer, not down at the query layer, so there's no SQL-injection surface from a report
/// code or date range.
/// </summary>
public class ReportExecutionService(IAppDbContext db, ICurrentUser currentUser, PermissionService permissions)
{
    public async Task<ReportResultDto> ExecuteAsync(string code, ReportExecuteRequest request, CancellationToken ct = default)
    {
        var def = await db.ReportDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.Code == code && d.IsActive, ct)
            ?? throw new DomainException($"No such report '{code}'.");
        if (def.ViewName is null)
        {
            throw new DomainException($"Report '{code}' has no executable view — open it from its own page ({def.LinkPath}).");
        }
        if (!await permissions.HasPermissionAsync(currentUser.UserId, def.PermissionCode, ct))
        {
            throw new UnauthorizedAccessException($"You don't have permission to run '{code}'.");
        }

        var crossBranch = request.CrossBranch && await permissions.HasPermissionAsync(currentUser.UserId, PermissionRegistry.ReportCrossBranchView, ct);
        var branchId = currentUser.BranchId;
        if (!crossBranch && branchId is null)
        {
            throw new DomainException("Select a branch first.");
        }
        var range = new ReportDateRange(request.FromDate, request.ToDate);

        var rows = def.ViewName switch
        {
            "mv_revenue_daily" => await QueryRevenueDailyAsync(range, crossBranch, branchId, ct),
            "mv_daily_collection" => await QueryDailyCollectionAsync(range, crossBranch, branchId, ct),
            "mv_op_census" => await QueryOpCensusAsync(range, crossBranch, branchId, ct),
            _ => throw new DomainException($"Report '{code}' points at an unknown view '{def.ViewName}'."),
        };

        var columns = JsonSerializer.Deserialize<List<ReportColumnDto>>(def.ColumnsJson, ReportJsonOptions.CaseInsensitive) ?? [];
        return new ReportResultDto(def.Name, columns, rows);
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryRevenueDailyAsync(
        ReportDateRange range, bool crossBranch, long? branchId, CancellationToken ct)
    {
        var rows = await ReportRangeFilters.InRange(db.RevenueDailyRows.AsNoTracking(), range)
            .Where(r => crossBranch || r.BranchId == branchId)
            .OrderBy(r => r.RevenueDate).ToListAsync(ct);
        return rows.Select(r => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["branchId"] = r.BranchId,
            ["revenueDate"] = r.RevenueDate.ToString("yyyy-MM-dd"),
            ["departmentName"] = r.DepartmentName ?? "Unassigned",
            ["doctorName"] = r.DoctorName ?? "—",
            ["amount"] = r.Amount,
        }).ToList();
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryDailyCollectionAsync(
        ReportDateRange range, bool crossBranch, long? branchId, CancellationToken ct)
    {
        var rows = await ReportRangeFilters.InRange(db.DailyCollectionRows.AsNoTracking(), range)
            .Where(r => crossBranch || r.BranchId == branchId)
            .OrderBy(r => r.CollectionDate).ToListAsync(ct);
        return rows.Select(r => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["branchId"] = r.BranchId,
            ["collectionDate"] = r.CollectionDate.ToString("yyyy-MM-dd"),
            ["paymentModeName"] = r.PaymentModeName,
            ["amount"] = r.Amount,
            ["txnCount"] = r.TxnCount,
        }).ToList();
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryOpCensusAsync(
        ReportDateRange range, bool crossBranch, long? branchId, CancellationToken ct)
    {
        var rows = await ReportRangeFilters.InRange(db.OpCensusRows.AsNoTracking(), range)
            .Where(r => crossBranch || r.BranchId == branchId)
            .OrderBy(r => r.VisitDate).ToListAsync(ct);
        return rows.Select(r => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["branchId"] = r.BranchId,
            ["visitDate"] = r.VisitDate.ToString("yyyy-MM-dd"),
            ["doctorName"] = r.DoctorName,
            ["visitType"] = r.VisitType == 1 ? "New" : "Review",
            ["visitCount"] = r.VisitCount,
        }).ToList();
    }
}
