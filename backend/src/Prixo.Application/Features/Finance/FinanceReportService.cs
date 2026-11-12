using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Finance;

/// <summary>
/// Finance reports (plan §18: Cash &amp; Bank Collection, Revenue, GST/Tax, Outstanding, and
/// the centralized cross-branch Day Close console) — all read-only aggregates, same posture
/// as MrdReportService. The day-close console is the only cross-branch read here; it goes
/// through IgnoreQueryFilters() and is gated by REPORT.CROSSBRANCH.VIEW at the controller.
/// </summary>
public class FinanceReportService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<CashBankCollectionDto> GetCashBankCollectionAsync(FinanceReportDateRange range, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var payments = db.BillPayments.AsNoTracking().Where(p => p.Bill!.BranchId == branchId);
        if (range.FromDate is { } from) payments = payments.Where(p => p.PaidAt >= AtStart(from));
        if (range.ToDate is { } to) payments = payments.Where(p => p.PaidAt < AtStart(to.AddDays(1)));

        var rows = await payments.GroupBy(p => new { p.PaymentModeId, p.PaymentMode!.Name })
            .Select(g => new CashBankCollectionRowDto(g.Key.PaymentModeId, g.Key.Name, g.Sum(p => p.Amount), g.Count()))
            .ToListAsync(ct);
        return new CashBankCollectionDto(rows, rows.Sum(r => r.Amount));
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(FinanceReportDateRange range, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var items = db.BillItems.AsNoTracking().Where(i => i.Bill!.BranchId == branchId && i.Bill.Status != BillStatus.Cancelled);
        if (range.FromDate is { } from) items = items.Where(i => i.Bill!.BillDate >= from);
        if (range.ToDate is { } to) items = items.Where(i => i.Bill!.BillDate <= to);

        var byDepartment = await items
            .GroupBy(i => new { i.Service!.DepartmentId, DepartmentName = i.Service.Department != null ? i.Service.Department.Name : "Unassigned" })
            .Select(g => new RevenueByDepartmentRowDto(g.Key.DepartmentId, g.Key.DepartmentName, g.Sum(i => i.TotalAmount)))
            .ToListAsync(ct);

        var byDoctor = await items
            .Where(i => i.Bill!.OpVisit != null)
            .GroupBy(i => new { i.Bill!.OpVisit!.DoctorId, DoctorName = i.Bill.OpVisit.Doctor!.User!.FullName })
            .Select(g => new RevenueByDoctorRowDto(g.Key.DoctorId, g.Key.DoctorName, g.Sum(i => i.TotalAmount)))
            .ToListAsync(ct);

        return new RevenueReportDto(byDepartment, byDoctor);
    }

    public async Task<GstReportDto> GetGstReportAsync(FinanceReportDateRange range, CancellationToken ct = default)
    {
        var branchId = RequireBranch();

        var billItems = db.BillItems.AsNoTracking().Where(i => i.Bill!.BranchId == branchId && i.Bill.Status != BillStatus.Cancelled && i.TaxAmount > 0);
        if (range.FromDate is { } bFrom) billItems = billItems.Where(i => i.Bill!.BillDate >= bFrom);
        if (range.ToDate is { } bTo) billItems = billItems.Where(i => i.Bill!.BillDate <= bTo);
        var billRows = await billItems.GroupBy(i => i.TaxPercent)
            .Select(g => new GstOutwardRowDto("Billing", g.Key, g.Sum(i => i.TotalAmount - i.TaxAmount), g.Sum(i => i.TaxAmount)))
            .ToListAsync(ct);

        var saleItems = db.PharmacySaleItems.AsNoTracking()
            .Where(i => i.PharmacySale!.BranchId == branchId && i.PharmacySale.Status != PharmacySaleStatus.Cancelled && i.TaxAmount > 0);
        if (range.FromDate is { } sFrom) saleItems = saleItems.Where(i => i.PharmacySale!.SaleDate >= sFrom);
        if (range.ToDate is { } sTo) saleItems = saleItems.Where(i => i.PharmacySale!.SaleDate <= sTo);
        var saleRows = await saleItems.GroupBy(i => i.TaxPercent)
            .Select(g => new GstOutwardRowDto("Pharmacy", g.Key, g.Sum(i => i.TotalAmount - i.TaxAmount), g.Sum(i => i.TaxAmount)))
            .ToListAsync(ct);

        var hsnSummary = await saleItems
            .Select(i => new { Hsn = i.Batch!.Drug != null ? i.Batch.Drug.HsnCode : i.Batch.Item!.HsnCode, i.Quantity, i.TaxAmount, i.TotalAmount })
            .Where(x => x.Hsn != null)
            .GroupBy(x => x.Hsn!)
            .Select(g => new HsnSummaryRowDto(g.Key, g.Sum(x => x.Quantity), g.Sum(x => x.TotalAmount - x.TaxAmount), g.Sum(x => x.TaxAmount)))
            .ToListAsync(ct);

        return new GstReportDto([.. billRows, .. saleRows], hsnSummary);
    }

    public async Task<OutstandingReportDto> GetOutstandingReportAsync(CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var rows = await db.Bills.AsNoTracking()
            .Where(b => b.BranchId == branchId && b.Status != BillStatus.Cancelled && b.BalanceAmount > 0)
            .Include(b => b.Patient).Include(b => b.Corporate)
            .OrderByDescending(b => b.BillDate)
            .Select(b => new OutstandingBillRowDto(
                b.Id, b.BillNumber, b.BillDate, b.Patient!.FirstName + " " + b.Patient.LastName,
                b.IsCreditBill, b.Corporate != null ? b.Corporate.Name : null, b.TotalAmount, b.BalanceAmount))
            .ToListAsync(ct);
        return new OutstandingReportDto(rows, rows.Sum(r => r.BalanceAmount));
    }

    /// <summary>
    /// Every account's cumulative Debit/Credit totals from every LedgerEntry posted on or
    /// before asOfDate — the classic "does the ledger balance" check (TotalDebit ==
    /// TotalCredit across the whole result), not a period-activity report. Accounts with no
    /// ledger activity at all are omitted, same as a real trial balance only lists accounts
    /// that have been posted to.
    /// </summary>
    public async Task<TrialBalanceDto> GetTrialBalanceAsync(DateOnly asOfDate, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var rows = await db.LedgerEntries.AsNoTracking()
            .Where(e => e.BranchId == branchId && e.EntryDate <= asOfDate)
            .GroupBy(e => new { e.AccountId, e.Account!.Code, e.Account.Name, e.Account.AccountType })
            .Select(g => new TrialBalanceRowDto(
                g.Key.AccountId, g.Key.Code, g.Key.Name, g.Key.AccountType.ToString(), g.Sum(e => e.DebitAmount), g.Sum(e => e.CreditAmount)))
            .ToListAsync(ct);
        var ordered = rows.OrderBy(r => r.AccountCode).ToList();
        return new TrialBalanceDto(asOfDate, ordered, ordered.Sum(r => r.DebitTotal), ordered.Sum(r => r.CreditTotal));
    }

    /// <summary>The centralized "all counters" day-close console (plan §18) — every branch's status for one date. Cross-branch: caller must hold REPORT.CROSSBRANCH.VIEW.</summary>
    public async Task<IReadOnlyList<DayCloseConsoleRowDto>> GetDayCloseConsoleAsync(DateOnly date, CancellationToken ct = default)
    {
        var branches = await db.Branches.AsNoTracking().IgnoreQueryFilters().Where(b => !b.IsDeleted).ToListAsync(ct);
        var closes = await db.DayCloses.AsNoTracking().IgnoreQueryFilters()
            .Where(d => d.Date == date).Include(d => d.ModeAmounts)
            .ToListAsync(ct);

        return branches.Select(b =>
        {
            var close = closes.FirstOrDefault(c => c.BranchId == b.Id);
            if (close is null) return new DayCloseConsoleRowDto(b.Id, b.Name, false, null, null, null);
            return new DayCloseConsoleRowDto(b.Id, b.Name, true,
                close.ModeAmounts.Sum(m => m.SystemAmount), close.ModeAmounts.Sum(m => m.DeclaredAmount), close.ModeAmounts.Sum(m => m.Variance));
        }).ToList();
    }

    private static DateTimeOffset AtStart(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
