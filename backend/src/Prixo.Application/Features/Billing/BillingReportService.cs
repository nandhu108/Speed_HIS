using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Auth;
using Prixo.Application.Features.Ipd;
using Prixo.Application.Features.Reports;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Billing;

/// <summary>
/// Read-only Billing reports beyond the Custom Report Builder's day-aggregated materialized
/// views (plan §23) — these are row-level detail reports over live tables (Bill/BillItem/
/// DiscountApproval/Refund/Advance/Tariff/DoctorPayoutRule/CashierShiftModeAmount), same
/// posture as FinanceReportService/MrdReportService. No new schema; every query here reuses
/// entities earlier phases and batches of this billing extension already built. Every
/// report touching Bill/BillItem is narrowed to the caller's BILLING.BILL.VIEWOP/VIEWIP/
/// VIEWEMERGENCY set (same RBAC bill-type scoping as BillingService) — Advance/Tariff/
/// CashierShift-based reports aren't bill-type-specific and stay unscoped.
/// </summary>
public class BillingReportService(IAppDbContext db, ICurrentUser currentUser, PermissionService permissions, TimeProvider timeProvider)
{
    /// <summary>Legacy "Ageing Report As On Date" — asOnDate is the reference date buckets are computed against (defaults to today), not a filter on which bills appear; every still-outstanding bill as of that date always shows.</summary>
    public async Task<AgeingReportDto> GetAgeingReportAsync(DateOnly? asOnDate, BillType? billType = null, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var allowedTypes = await GetAllowedBillTypesAsync(ct);
        var typesToUse = billType is { } t ? (allowedTypes.Contains(t) ? new HashSet<BillType> { t } : []) : allowedTypes;
        var today = asOnDate ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var bills = await db.Bills.AsNoTracking()
            .Where(b => b.BranchId == branchId && b.Status != BillStatus.Cancelled && b.BalanceAmount > 0 && typesToUse.Contains(b.BillType) && b.BillDate <= today)
            .Include(b => b.Patient)
            .OrderBy(b => b.BillDate)
            .ToListAsync(ct);

        var rows = bills.Select(b =>
        {
            var days = today.DayNumber - b.BillDate.DayNumber;
            var bucket = days switch
            {
                <= 30 => "0-30 days",
                <= 60 => "31-60 days",
                <= 90 => "61-90 days",
                _ => "90+ days",
            };
            return new AgeingBillRowDto(b.Id, b.BillNumber, b.BillDate, b.Patient!.FirstName + " " + b.Patient.LastName, b.BalanceAmount, days, bucket);
        }).ToList();

        return new AgeingReportDto(rows, rows.Sum(r => r.BalanceAmount));
    }

    /// <summary>
    /// Outstanding balance on IP bills, grouped by the admission's current ward — inherently
    /// IP-only, so requires BILLING.BILL.VIEWIP. isCredit narrows to overpaid bills (negative
    /// balance — the hospital owes the patient, legacy "CREDIT") vs still-owed bills (positive
    /// balance, legacy "DEBIT"); omitted, both count (previously this only ever showed positive
    /// balances, silently dropping overpaid admissions entirely). departmentId matches any of
    /// the admission's primary consultant's departments (Doctor.DoctorDepartments — a doctor
    /// can belong to more than one), since a ward itself has no department of its own here.
    /// </summary>
    public async Task<WardWiseOutstandingReportDto> GetWardWiseOutstandingAsync(
        string? ipNumber = null, long? wardId = null, long? corporateId = null, long? patientCategoryId = null,
        long? departmentId = null, bool? isCredit = null, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        await RequireIpAccessAsync(ct);
        var bills = BuildWardOutstandingQuery(branchId, ipNumber, wardId, corporateId, patientCategoryId, departmentId, isCredit);

        // GroupBy immediately after a Select through a 4-level join chain (Bill -> Admission ->
        // Bed -> Room -> Ward) isn't translatable by EF Core's query compiler (it flattens the
        // projection away and then can't translate the grouped Sum/Count over the resulting
        // TransparentIdentifier) — 500s every time. Materialize the flat rows first (bounded by
        // "currently outstanding admitted bills", not the whole Bills table) and group in memory,
        // same posture as GetAgeingReportAsync's bucket summary above.
        var flat = await bills
            .Select(b => new { b.BalanceAmount, WardName = b.Admission!.CurrentBed!.Room!.Ward!.Name })
            .ToListAsync(ct);

        var rows = flat
            .GroupBy(b => b.WardName)
            .Select(g => new WardOutstandingRowDto(g.Key, g.Sum(x => x.BalanceAmount), g.Count()))
            .OrderByDescending(w => w.BalanceAmount)
            .ToList();

        return new WardWiseOutstandingReportDto(rows, rows.Sum(r => r.BalanceAmount));
    }

    private IQueryable<Bill> BuildWardOutstandingQuery(
        long branchId, string? ipNumber, long? wardId, long? corporateId, long? patientCategoryId, long? departmentId, bool? isCredit)
    {
        var bills = db.Bills.AsNoTracking()
            .Where(b => b.BranchId == branchId && b.Status != BillStatus.Cancelled && b.BalanceAmount != 0 && b.AdmissionId != null);
        if (!string.IsNullOrWhiteSpace(ipNumber)) bills = bills.Where(b => EF.Functions.ILike(b.Admission!.AdmissionNumber, $"%{ipNumber.Trim()}%"));
        if (wardId is { } wid) bills = bills.Where(b => b.Admission!.CurrentBed!.Room!.WardId == wid);
        if (corporateId is { } cid) bills = bills.Where(b => b.CorporateId == cid);
        if (patientCategoryId is { } pcid) bills = bills.Where(b => b.Patient!.PatientCategoryId == pcid);
        if (departmentId is { } did) bills = bills.Where(b => b.Admission!.PrimaryConsultant!.DoctorDepartments.Any(dd => dd.DepartmentId == did));
        if (isCredit is { } credit) bills = bills.Where(b => credit ? b.BalanceAmount < 0 : b.BalanceAmount > 0);
        return bills;
    }

    /// <summary>
    /// Report Type "detail" — one row per admission instead of GetWardWiseOutstandingAsync's
    /// per-ward aggregate, same filters. See WardOutstandingDetailRowDto for why this is a
    /// RoomCharge/Service 2-way split rather than the legacy screen's Investigation/Pharmacy/
    /// Other-Service 3-way one.
    /// </summary>
    public async Task<WardOutstandingDetailReportDto> GetWardWiseOutstandingDetailAsync(
        string? ipNumber = null, long? wardId = null, long? corporateId = null, long? patientCategoryId = null,
        long? departmentId = null, bool? isCredit = null, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        await RequireIpAccessAsync(ct);
        var bills = BuildWardOutstandingQuery(branchId, ipNumber, wardId, corporateId, patientCategoryId, departmentId, isCredit);

        var billRows = await bills
            .Include(b => b.Patient)
            .Include(b => b.Admission!).ThenInclude(a => a.CurrentBed!).ThenInclude(bed => bed.Room!).ThenInclude(r => r.Ward)
            .Include(b => b.Admission!).ThenInclude(a => a.PrimaryConsultant!).ThenInclude(d => d!.DoctorDepartments).ThenInclude(dd => dd.Department)
            .Include(b => b.Corporate)
            .Include(b => b.Items)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var admissionIds = billRows.Select(b => b.AdmissionId!.Value).Distinct().ToList();
        var advanceByAdmission = await db.Advances.AsNoTracking()
            .Where(a => a.AdmissionId != null && admissionIds.Contains(a.AdmissionId.Value))
            .GroupBy(a => a.AdmissionId!.Value)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(a => a.BalanceAmount), ct);

        var rows = billRows.Select(b =>
        {
            var admission = b.Admission!;
            var roomCharge = b.Items.Where(i => i.SourceType == BillItemSourceType.BedCharge).Sum(i => i.TotalAmount);
            var serviceAmount = b.Items.Where(i => i.SourceType != BillItemSourceType.BedCharge).Sum(i => i.TotalAmount);
            var departmentName = admission.PrimaryConsultant?.DoctorDepartments.Select(dd => dd.Department?.Name).FirstOrDefault(n => n != null);
            return new WardOutstandingDetailRowDto(
                b.Patient!.Uhid, IpdMath.FormatAge(b.Patient.DateOfBirth, b.Patient.EstimatedAge, today), admission.AdmissionNumber,
                b.Patient.FirstName + " " + b.Patient.LastName, admission.AdmissionDate, admission.CurrentBed!.Code,
                admission.CurrentBed.Room!.Ward!.Name, departmentName, roomCharge, serviceAmount, b.TotalAmount,
                advanceByAdmission.GetValueOrDefault(admission.Id), b.BalanceAmount, b.BalanceAmount < 0, b.Corporate?.Name);
        })
        .OrderByDescending(r => Math.Abs(r.BalanceAmount))
        .ToList();

        return new WardOutstandingDetailReportDto(rows, rows.Sum(r => r.BalanceAmount));
    }

    /// <summary>
    /// billType/billedByUserId/corporateId are optional narrowing filters on top of the
    /// caller's own allowed bill types — a billType outside that allowed set (rather than
    /// throwing) just yields an empty report, same posture as an unauthorized cross-branch
    /// read returning nothing instead of a 403 mid-report. billedByUserId matches each row's
    /// own creator (the bill's biller / the payment's collecting cashier — BaseEntity.CreatedBy
    /// on each, not necessarily the same person for a given bill/payment pair).
    /// </summary>
    public async Task<DayBookReportDto> GetDayBookReportAsync(
        DateOnly date, BillType? billType = null, long? billedByUserId = null, long? corporateId = null, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var allowedTypes = await GetAllowedBillTypesAsync(ct);
        var typesToUse = billType is { } t ? (allowedTypes.Contains(t) ? new HashSet<BillType> { t } : []) : allowedTypes;

        var bills = await db.Bills.AsNoTracking()
            .Where(b => b.BranchId == branchId && b.BillDate == date && typesToUse.Contains(b.BillType)
                && (corporateId == null || b.CorporateId == corporateId)
                && (billedByUserId == null || b.CreatedBy == billedByUserId))
            .Include(b => b.Patient)
            .Select(b => new DayBookBillRowDto(b.BillNumber, b.Patient!.FirstName + " " + b.Patient.LastName, b.TotalAmount))
            .ToListAsync(ct);

        var rangeStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var rangeEnd = rangeStart.AddDays(1);
        var payments = await db.BillPayments.AsNoTracking()
            .Where(p => p.Bill!.BranchId == branchId && p.PaidAt >= rangeStart && p.PaidAt < rangeEnd && typesToUse.Contains(p.Bill.BillType)
                && (corporateId == null || p.Bill.CorporateId == corporateId)
                && (billedByUserId == null || p.CreatedBy == billedByUserId))
            .Include(p => p.Bill).ThenInclude(b => b!.Patient)
            .Include(p => p.PaymentMode)
            .Select(p => new DayBookPaymentRowDto(
                p.ReceiptNumber, p.Bill!.BillNumber, p.Bill.Patient!.FirstName + " " + p.Bill.Patient.LastName,
                p.PaymentMode!.Name, p.Amount, p.PaidAt))
            .ToListAsync(ct);

        return new DayBookReportDto(date, bills, payments, bills.Sum(b => b.TotalAmount), payments.Sum(p => p.Amount));
    }

    public async Task<CancelledBillReportDto> GetCancelledBillsReportAsync(BillingReportDateRange range, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var allowedTypes = await GetAllowedBillTypesAsync(ct);
        var bills = db.Bills.AsNoTracking().Where(b => b.BranchId == branchId && b.Status == BillStatus.Cancelled && allowedTypes.Contains(b.BillType));
        if (range.FromDate is { } from) bills = bills.Where(b => b.BillDate >= from);
        if (range.ToDate is { } to) bills = bills.Where(b => b.BillDate <= to);

        var rows = await bills.Include(b => b.Patient).OrderByDescending(b => b.CancelledAt)
            .Select(b => new CancelledBillRowDto(
                b.Id, b.BillNumber, b.BillDate, b.Patient!.FirstName + " " + b.Patient.LastName, b.TotalAmount, b.CancelReason, b.CancelledAt))
            .ToListAsync(ct);

        return new CancelledBillReportDto(rows);
    }

    /// <summary>
    /// Backs the "OP Bill Concession Search" screen — one menu item per report, this one a
    /// genuine search (Reg No/Patient Name/OT No/Bill No), not just a date-ranged list.
    /// </summary>
    public async Task<PagedResult<ConcessionSearchRowDto>> SearchConcessionsAsync(ConcessionSearchQuery query, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var allowedTypes = await GetAllowedBillTypesAsync(ct);
        var approvals = BuildConcessionQuery(branchId, allowedTypes, query.FromDate, query.ToDate, query.RegNo, query.PatientName, query.BillNo, query.TokenNo)
            .OrderByDescending(a => a.DecidedAt);

        var totalCount = await approvals.CountAsync(ct);
        var page = approvals.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize);
        var rows = await ToConcessionRowsAsync(page, ct);

        return PagedResult<ConcessionSearchRowDto>.Create(rows, query.Page, query.PageSize, totalCount);
    }

    /// <summary>Shared by SearchConcessionsAsync (paged, on-screen) and the "concession" export case (unpaged, date-range only) so the two filter sets never drift — same posture as BillingService.FilterBills.</summary>
    private IQueryable<DiscountApproval> BuildConcessionQuery(
        long branchId, IReadOnlyCollection<BillType> allowedTypes, DateOnly? fromDate, DateOnly? toDate,
        string? regNo = null, string? patientName = null, string? billNo = null, int? tokenNo = null)
    {
        var approvals = db.DiscountApprovals.AsNoTracking()
            .Where(a => a.Bill!.BranchId == branchId && a.Status == DiscountApprovalStatus.Approved && allowedTypes.Contains(a.Bill.BillType));
        if (fromDate is { } from) approvals = approvals.Where(a => a.DecidedAt >= new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (toDate is { } to) approvals = approvals.Where(a => a.DecidedAt < new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (!string.IsNullOrWhiteSpace(regNo)) approvals = approvals.Where(a => EF.Functions.ILike(a.Bill!.Patient!.Uhid, $"%{regNo.Trim()}%"));
        if (!string.IsNullOrWhiteSpace(patientName))
        {
            var term = $"%{patientName.Trim()}%";
            approvals = approvals.Where(a => EF.Functions.ILike(a.Bill!.Patient!.FirstName, term) || EF.Functions.ILike(a.Bill.Patient.LastName, term));
        }
        if (!string.IsNullOrWhiteSpace(billNo)) approvals = approvals.Where(a => EF.Functions.ILike(a.Bill!.BillNumber, $"%{billNo.Trim()}%"));
        if (tokenNo is { } token) approvals = approvals.Where(a => a.Bill!.OpVisit != null && a.Bill.OpVisit.TokenNumber == token);
        return approvals;
    }

    /// <summary>Resolves an (already filtered, optionally paged) DiscountApproval query into display rows, including the Requested-by/Approved-by user names (CreatedBy/DecidedByUserId carry no navigation property, so resolved via one batched Users lookup rather than N+1).</summary>
    private async Task<List<ConcessionSearchRowDto>> ToConcessionRowsAsync(IQueryable<DiscountApproval> approvals, CancellationToken ct)
    {
        var list = await approvals
            .Include(a => a.Bill).ThenInclude(b => b!.Patient)
            .Include(a => a.Bill).ThenInclude(b => b!.OpVisit).ThenInclude(v => v!.Doctor).ThenInclude(d => d!.User)
            .Include(a => a.Bill).ThenInclude(b => b!.OpVisit).ThenInclude(v => v!.Doctor).ThenInclude(d => d!.Specialty)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var userIds = list.SelectMany(a => new long?[] { a.CreatedBy, a.DecidedByUserId }).Where(id => id != null).Select(id => id!.Value).Distinct().ToList();
        var userNames = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        return list.Select(a => new ConcessionSearchRowDto(
            a.BillId, a.Bill!.BillNumber, a.Bill.BillDate, a.Bill.Patient!.Uhid, a.Bill.Patient.FirstName + " " + a.Bill.Patient.LastName,
            IpdMath.FormatAge(a.Bill.Patient.DateOfBirth, a.Bill.Patient.EstimatedAge, today), a.Bill.Patient.Gender,
            a.Bill.OpVisit?.Doctor?.User?.FullName, a.Bill.OpVisit?.Doctor?.Specialty?.Name,
            a.Bill.TotalAmount, a.RequestedPercent, a.RequestedAmount, a.Reason,
            userNames.GetValueOrDefault(a.CreatedBy, "—"), a.CreatedAt,
            a.DecidedByUserId is { } decidedBy ? userNames.GetValueOrDefault(decidedBy) : null, a.DecidedAt))
            .ToList();
    }

    /// <summary>
    /// billType/corporateId only narrow bill refunds (an advance refund has neither dimension)
    /// — with either filter set, advance refunds are excluded from the result entirely, same
    /// "doesn't apply to this row shape, so it drops out" posture as other cross-flavor reports
    /// in this file.
    /// </summary>
    public async Task<RefundReportDto> GetRefundReportAsync(
        BillingReportDateRange range, BillType? billType = null, long? corporateId = null, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var allowedTypes = await GetAllowedBillTypesAsync(ct);
        var typesToUse = billType is { } t ? (allowedTypes.Contains(t) ? new HashSet<BillType> { t } : []) : allowedTypes;
        var includeAdvanceRefunds = billType is null && corporateId is null;

        var refunds = db.Refunds.AsNoTracking().Where(r =>
            (r.BillId != null && r.Bill!.BranchId == branchId && typesToUse.Contains(r.Bill.BillType) && (corporateId == null || r.Bill.CorporateId == corporateId))
            || (includeAdvanceRefunds && r.AdvanceId != null && r.Advance!.BranchId == branchId));
        if (range.FromDate is { } from) refunds = refunds.Where(r => r.CreatedAt >= new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (range.ToDate is { } to) refunds = refunds.Where(r => r.CreatedAt < new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

        var rows = await refunds
            .Include(r => r.Bill).ThenInclude(b => b!.Patient)
            .Include(r => r.BillPayment)
            .Include(r => r.Advance).ThenInclude(a => a!.Patient)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RefundReportRowDto(
                r.BillId, r.Bill != null ? r.Bill.BillNumber : null, r.AdvanceId,
                r.Bill != null ? r.Bill.Patient!.Uhid : r.Advance!.Patient!.Uhid,
                r.Bill != null ? r.Bill.Patient!.FirstName + " " + r.Bill.Patient.LastName : r.Advance!.Patient!.FirstName + " " + r.Advance.Patient.LastName,
                r.BillPayment != null ? r.BillPayment.ReceiptNumber : null,
                r.Amount, r.Reason, r.Status.ToString(), r.CreatedAt))
            .ToListAsync(ct);

        return new RefundReportDto(rows, rows.Where(r => r.Status == nameof(RefundStatus.Issued)).Sum(r => r.Amount));
    }

    public async Task<AdvanceReportDto> GetAdvanceReportAsync(BillingReportDateRange range, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var advances = db.Advances.AsNoTracking().Where(a => a.BranchId == branchId);
        if (range.FromDate is { } from) advances = advances.Where(a => a.ReceivedAt >= new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (range.ToDate is { } to) advances = advances.Where(a => a.ReceivedAt < new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

        var rows = await advances.Include(a => a.Patient).Include(a => a.PaymentMode).OrderByDescending(a => a.ReceivedAt)
            .Select(a => new AdvanceReportRowDto(
                a.Patient!.FirstName + " " + a.Patient.LastName, a.Patient.Uhid, a.Amount, a.BalanceAmount, a.PaymentMode!.Name, a.ReceivedAt))
            .ToListAsync(ct);

        return new AdvanceReportDto(rows, rows.Sum(r => r.Amount), rows.Sum(r => r.BalanceAmount));
    }

    public async Task<TariffListReportDto> GetTariffListReportAsync(CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var rows = await db.TariffItems.AsNoTracking()
            .Where(ti => ti.Tariff!.BranchId == branchId)
            .Include(ti => ti.Tariff).ThenInclude(t => t!.PatientCategory)
            .Include(ti => ti.Service)
            .OrderBy(ti => ti.Tariff!.Name).ThenBy(ti => ti.Service!.Name)
            .Select(ti => new TariffListRowDto(
                ti.Tariff!.Name, ti.Tariff.PatientCategory != null ? ti.Tariff.PatientCategory.Name : null,
                ti.Service!.Name, ti.Rate, ti.Tariff.EffectiveFrom, ti.Tariff.EffectiveTo))
            .ToListAsync(ct);

        return new TariffListReportDto(rows);
    }

    /// <summary>
    /// Payout per (doctor, service category) — matches each Service-sourced BillItem to the
    /// bill's OP doctor (OpVisit.Doctor) or IP consultant (Admission.PrimaryConsultant), then
    /// looks up that doctor's DoctorPayoutRule for the item's ServiceCategory. Package lines
    /// have no single service category and are excluded — an honest scoping call, not an
    /// oversight.
    /// </summary>
    public async Task<DoctorPayoutReportDto> GetDoctorsPayoutReportAsync(BillingReportDateRange range, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var allowedTypes = await GetAllowedBillTypesAsync(ct);
        var items = db.BillItems.AsNoTracking()
            .Where(i => i.Bill!.BranchId == branchId && i.Bill.Status != BillStatus.Cancelled
                && i.SourceType == BillItemSourceType.Service && i.ServiceId != null && allowedTypes.Contains(i.Bill.BillType));
        if (range.FromDate is { } from) items = items.Where(i => i.Bill!.BillDate >= from);
        if (range.ToDate is { } to) items = items.Where(i => i.Bill!.BillDate <= to);

        var lines = await items
            .Select(i => new
            {
                DoctorId = i.Bill!.OpVisitId != null ? i.Bill.OpVisit!.DoctorId : (i.Bill.AdmissionId != null ? i.Bill.Admission!.PrimaryConsultantId : (long?)null),
                ServiceCategoryId = i.Service!.ServiceCategoryId,
                ServiceCategoryName = i.Service.Category!.Name,
                i.TotalAmount,
            })
            .Where(x => x.DoctorId != null)
            .ToListAsync(ct);

        var rules = await db.DoctorPayoutRules.AsNoTracking()
            .Where(r => r.BranchId == branchId && r.IsActive)
            .Include(r => r.Doctor).ThenInclude(d => d!.User)
            .ToListAsync(ct);

        var rows = lines
            .GroupBy(l => new { l.DoctorId, l.ServiceCategoryId, l.ServiceCategoryName })
            .Select(g =>
            {
                var rule = rules.FirstOrDefault(r => r.DoctorId == g.Key.DoctorId && r.ServiceCategoryId == g.Key.ServiceCategoryId);
                var billedAmount = g.Sum(x => x.TotalAmount);
                var payout = rule is null
                    ? 0
                    : rule.PayoutType == PayoutType.Percent ? billedAmount * rule.Value / 100 : rule.Value * g.Count();
                var doctorName = rule?.Doctor?.User?.FullName ?? "Unassigned";
                return new DoctorPayoutRowDto(g.Key.DoctorId!.Value, doctorName, g.Key.ServiceCategoryName, billedAmount, payout);
            })
            .Where(r => r.PayoutAmount > 0)
            .OrderByDescending(r => r.PayoutAmount)
            .ToList();

        return new DoctorPayoutReportDto(rows, rows.Sum(r => r.PayoutAmount));
    }

    public async Task<UserSettlementReportDto> GetUserSettlementReportAsync(BillingReportDateRange range, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var amounts = db.CashierShiftModeAmounts.AsNoTracking().Where(m => m.CashierShift!.BranchId == branchId);
        if (range.FromDate is { } from) amounts = amounts.Where(m => m.CashierShift!.ClosedAt >= new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (range.ToDate is { } to) amounts = amounts.Where(m => m.CashierShift!.ClosedAt < new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

        var rows = await amounts
            .Include(m => m.CashierShift).ThenInclude(s => s!.User)
            .Include(m => m.PaymentMode)
            .OrderByDescending(m => m.CashierShift!.ClosedAt)
            .Select(m => new UserSettlementRowDto(
                m.CashierShift!.User!.FullName, m.CashierShift.ClosedAt ?? m.CashierShift.OpenedAt,
                m.PaymentMode!.Name, m.SystemAmount, m.DeclaredAmount, m.Variance))
            .ToListAsync(ct);

        return new UserSettlementReportDto(rows);
    }

    /// <summary>"Header-wise Collection Report" — billed amount grouped by Service.ServiceCategory ("header").</summary>
    public async Task<ServiceCategoryBreakupReportDto> GetServiceCategoryBreakupReportAsync(BillingReportDateRange range, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var allowedTypes = await GetAllowedBillTypesAsync(ct);
        var items = db.BillItems.AsNoTracking()
            .Where(i => i.Bill!.BranchId == branchId && i.Bill.Status != BillStatus.Cancelled
                && i.SourceType == BillItemSourceType.Service && i.ServiceId != null && allowedTypes.Contains(i.Bill.BillType));
        if (range.FromDate is { } from) items = items.Where(i => i.Bill!.BillDate >= from);
        if (range.ToDate is { } to) items = items.Where(i => i.Bill!.BillDate <= to);

        // Same EF Core translation failure as GetWardWiseOutstandingAsync — GroupBy right after
        // a Select through a join chain (BillItem -> Bill, BillItem -> Service -> ServiceCategory)
        // can't be translated to SQL and 500s every time. Materialize the flat (category, amount)
        // rows first and group in memory.
        var flat = await items
            .Select(i => new { CategoryName = i.Service!.Category!.Name, i.TotalAmount })
            .ToListAsync(ct);

        var rows = flat
            .GroupBy(i => i.CategoryName)
            .Select(g => new ServiceCategoryBreakupRowDto(g.Key, g.Sum(i => i.TotalAmount), g.Count()))
            .OrderByDescending(r => r.Amount)
            .ToList();

        return new ServiceCategoryBreakupReportDto(rows, rows.Sum(r => r.Amount));
    }

    /// <summary>Inherently IP-only, so requires BILLING.BILL.VIEWIP.</summary>
    public async Task<IpServiceBillingDetailReportDto> GetIpServiceBillingDetailReportAsync(BillingReportDateRange range, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        await RequireIpAccessAsync(ct);
        var items = db.BillItems.AsNoTracking()
            .Where(i => i.Bill!.BranchId == branchId && i.Bill.BillType == BillType.Ip && i.Bill.Status != BillStatus.Cancelled);
        if (range.FromDate is { } from) items = items.Where(i => i.Bill!.BillDate >= from);
        if (range.ToDate is { } to) items = items.Where(i => i.Bill!.BillDate <= to);

        var rows = await items.Include(i => i.Bill).ThenInclude(b => b!.Patient)
            .OrderByDescending(i => i.Bill!.BillDate)
            .Select(i => new IpServiceBillingDetailRowDto(
                i.Bill!.BillNumber, i.Bill.Patient!.FirstName + " " + i.Bill.Patient.LastName, i.Description, i.Quantity, i.Rate, i.TotalAmount))
            .ToListAsync(ct);

        return new IpServiceBillingDetailReportDto(rows, rows.Sum(r => r.TotalAmount));
    }

    /// <summary>
    /// "Revenue Report" — every non-cancelled bill item, narrowable by Bill Type/Company/
    /// Ordering Doctor/Service/Service Category ("Header")/Department/Paid-or-Credit on top of
    /// the date range, in either a Detail (one row per bill item) or Summary (grouped by
    /// Department) shape. Doctor resolves the same way as GetDoctorsPayoutReportAsync — the OP
    /// visit's doctor, or the IP admission's primary consultant; items on bills with neither
    /// (Emergency's manual-item path) have no doctor and are excluded only when a doctor filter
    /// is actively applied. Department/Service Category come from the item's own Service — a
    /// non-catalog manual/package line has neither and simply won't match those filters.
    /// </summary>
    public async Task<RevenueReportDto> GetRevenueReportAsync(
        BillingReportDateRange range, string? reportType, BillType? billType, long? corporateId, long? doctorId,
        long? serviceId, long? serviceCategoryId, long? departmentId, bool? isCredit, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var allowedTypes = await GetAllowedBillTypesAsync(ct);
        var typesToUse = billType is { } t ? (allowedTypes.Contains(t) ? new HashSet<BillType> { t } : []) : allowedTypes;

        var items = db.BillItems.AsNoTracking()
            .Where(i => i.Bill!.BranchId == branchId && i.Bill.Status != BillStatus.Cancelled && typesToUse.Contains(i.Bill.BillType));
        if (range.FromDate is { } from) items = items.Where(i => i.Bill!.BillDate >= from);
        if (range.ToDate is { } to) items = items.Where(i => i.Bill!.BillDate <= to);
        if (corporateId is { } cid) items = items.Where(i => i.Bill!.CorporateId == cid);
        if (isCredit is { } credit) items = items.Where(i => i.Bill!.IsCreditBill == credit);
        if (serviceId is { } sid) items = items.Where(i => i.ServiceId == sid);
        if (serviceCategoryId is { } scid) items = items.Where(i => i.Service != null && i.Service.ServiceCategoryId == scid);
        if (departmentId is { } did) items = items.Where(i => i.Service != null && i.Service.DepartmentId == did);

        var projected = await items
            .Select(i => new
            {
                i.Bill!.BillNumber, i.Bill.BillDate,
                PatientName = i.Bill.Patient!.FirstName + " " + i.Bill.Patient.LastName,
                DoctorId = i.Bill.OpVisitId != null ? i.Bill.OpVisit!.DoctorId
                    : i.Bill.AdmissionId != null ? i.Bill.Admission!.PrimaryConsultantId : (long?)null,
                DepartmentName = i.Service != null && i.Service.Department != null ? i.Service.Department.Name : null,
                ServiceCategoryName = i.Service != null && i.Service.Category != null ? i.Service.Category.Name : null,
                ServiceName = i.Service != null ? i.Service.Name : i.Description,
                i.Bill.IsCreditBill,
                i.TotalAmount,
            })
            .ToListAsync(ct);

        var filtered = doctorId is { } wantedDoctor ? projected.Where(x => x.DoctorId == wantedDoctor).ToList() : projected;

        if (string.Equals(reportType, "detail", StringComparison.OrdinalIgnoreCase))
        {
            var doctorIds = filtered.Where(x => x.DoctorId != null).Select(x => x.DoctorId!.Value).Distinct().ToList();
            var doctorNames = await db.Doctors.AsNoTracking().Where(d => doctorIds.Contains(d.Id))
                .Include(d => d.User).ToDictionaryAsync(d => d.Id, d => d.User!.FullName, ct);

            var rows = filtered
                .OrderByDescending(x => x.BillDate)
                .Select(x => new RevenueReportRowDto(
                    x.BillNumber, x.BillDate, x.PatientName,
                    x.DoctorId != null && doctorNames.TryGetValue(x.DoctorId.Value, out var name) ? name : null,
                    x.DepartmentName, x.ServiceCategoryName, x.ServiceName, null, x.TotalAmount, x.IsCreditBill))
                .ToList();
            return new RevenueReportDto(false, rows, rows.Sum(r => r.Amount));
        }
        else
        {
            var rows = filtered
                .GroupBy(x => x.DepartmentName ?? "Unassigned")
                .Select(g => new RevenueReportRowDto(null, null, null, null, g.Key, null, null, g.Count(), g.Sum(x => x.TotalAmount), null))
                .OrderByDescending(r => r.Amount)
                .ToList();
            return new RevenueReportDto(true, rows, rows.Sum(r => r.Amount));
        }
    }

    /// <summary>
    /// "Outstanding Report" — every non-cancelled bill with a non-zero balance, across every
    /// bill type (unlike Ward-wise Outstanding, not restricted to IP/admission-linked bills;
    /// unlike Ageing, no as-of-date bucketing — just a plain filtered list/summary). Summary
    /// groups by Bill Type; Detail is one row per bill.
    /// </summary>
    public async Task<OutstandingReportDto> GetOutstandingReportAsync(
        BillingReportDateRange range, string? reportType, BillType? billType, long? billedByUserId, long? corporateId, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var allowedTypes = await GetAllowedBillTypesAsync(ct);
        var typesToUse = billType is { } t ? (allowedTypes.Contains(t) ? new HashSet<BillType> { t } : []) : allowedTypes;

        var query = db.Bills.AsNoTracking()
            .Where(b => b.BranchId == branchId && b.Status != BillStatus.Cancelled && b.BalanceAmount != 0 && typesToUse.Contains(b.BillType));
        if (range.FromDate is { } from) query = query.Where(b => b.BillDate >= from);
        if (range.ToDate is { } to) query = query.Where(b => b.BillDate <= to);
        if (billedByUserId is { } uid) query = query.Where(b => b.CreatedBy == uid);
        if (corporateId is { } cid) query = query.Where(b => b.CorporateId == cid);

        var bills = await query.Include(b => b.Patient).Include(b => b.Corporate).ToListAsync(ct);
        static string BillTypeLabel(BillType t) => t switch { BillType.Op => "OP", BillType.Ip => "IP", BillType.Emergency => "Emergency", _ => t.ToString() };

        if (string.Equals(reportType, "detail", StringComparison.OrdinalIgnoreCase))
        {
            var rows = bills.OrderByDescending(b => b.BillDate)
                .Select(b => new OutstandingRowDto(
                    b.Patient!.Uhid, b.BillNumber, b.BillDate, b.Patient.FirstName + " " + b.Patient.LastName, BillTypeLabel(b.BillType),
                    b.Corporate?.Name, null, b.TotalAmount, b.PaidAmount, b.BalanceAmount, b.BalanceAmount < 0))
                .ToList();
            return new OutstandingReportDto(false, rows, rows.Sum(r => r.BalanceAmount));
        }
        else
        {
            var rows = bills.GroupBy(b => b.BillType)
                .Select(g => new OutstandingRowDto(null, null, null, null, BillTypeLabel(g.Key), null, g.Count(), null, null, g.Sum(x => x.BalanceAmount), null))
                .OrderByDescending(r => r.BalanceAmount)
                .ToList();
            return new OutstandingReportDto(true, rows, rows.Sum(r => r.BalanceAmount));
        }
    }

    /// <summary>
    /// Flattens any one of the reports above into the generic (Columns, Rows) shape
    /// Reports.ReportExportHelper needs, so every report here gets CSV/Excel/PDF export for
    /// free through the same helper the Custom Report Builder uses — one dispatcher instead
    /// of a bespoke export action per report.
    /// </summary>
    public async Task<ReportResultDto> GetReportResultAsync(
        string reportKey, BillingReportDateRange range, DateOnly? date,
        BillType? billType = null, long? billedByUserId = null, long? corporateId = null,
        long? doctorId = null, long? serviceId = null, long? serviceCategoryId = null, long? departmentId = null,
        bool? isCredit = null, string? reportType = null, string? ipNumber = null, long? wardId = null,
        long? patientCategoryId = null, CancellationToken ct = default)
    {
        switch (reportKey)
        {
            case "ageing":
            {
                var r = await GetAgeingReportAsync(date, billType, ct);
                if (string.Equals(reportType, "summary", StringComparison.OrdinalIgnoreCase))
                {
                    var bucketOrder = new[] { "0-30 days", "31-60 days", "61-90 days", "90+ days" };
                    var summaryRows = r.Bills.GroupBy(x => x.Bucket)
                        .OrderBy(g => Array.IndexOf(bucketOrder, g.Key))
                        .Select(g => new Dictionary<string, object?> { ["bucket"] = g.Key, ["billCount"] = g.Count(), ["balanceAmount"] = g.Sum(x => x.BalanceAmount) });
                    return Flatten("Ageing Report (Summary)", [("bucket", "Bucket"), ("billCount", "Bills"), ("balanceAmount", "Balance")], summaryRows);
                }
                return Flatten("Ageing Report",
                    [("billNumber", "Bill No"), ("billDate", "Date"), ("patientName", "Patient"), ("bucket", "Bucket"), ("balanceAmount", "Balance")],
                    r.Bills.Select(x => new Dictionary<string, object?>
                    {
                        ["billNumber"] = x.BillNumber, ["billDate"] = x.BillDate.ToString("yyyy-MM-dd"),
                        ["patientName"] = x.PatientName, ["bucket"] = x.Bucket, ["balanceAmount"] = x.BalanceAmount,
                    }));
            }
            case "ward-wise-outstanding":
            {
                if (string.Equals(reportType, "detail", StringComparison.OrdinalIgnoreCase))
                {
                    var rd = await GetWardWiseOutstandingDetailAsync(ipNumber, wardId, corporateId, patientCategoryId, departmentId, isCredit, ct);
                    return Flatten("Ward-wise Outstanding (Detail)",
                        [("regNo", "Reg No"), ("age", "Age"), ("ipNumber", "IP Number"), ("patientName", "Patient"),
                         ("admissionDate", "Admission Date"), ("bedCode", "Bed"), ("wardName", "Ward"), ("departmentName", "Department"),
                         ("roomChargeAmount", "Room Charges"), ("serviceAmount", "Service Amount"), ("billAmount", "Bill Amount"),
                         ("advanceAmount", "Advance"), ("balanceAmount", "Balance"), ("crdr", "CR/DR"), ("corporateName", "Corporate")],
                        rd.Rows.Select(x => new Dictionary<string, object?>
                        {
                            ["regNo"] = x.RegNo, ["age"] = x.Age, ["ipNumber"] = x.IpNumber, ["patientName"] = x.PatientName,
                            ["admissionDate"] = x.AdmissionDate.ToString("yyyy-MM-dd"), ["bedCode"] = x.BedCode, ["wardName"] = x.WardName,
                            ["departmentName"] = x.DepartmentName ?? "", ["roomChargeAmount"] = x.RoomChargeAmount,
                            ["serviceAmount"] = x.ServiceAmount, ["billAmount"] = x.BillAmount, ["advanceAmount"] = x.AdvanceAmount,
                            ["balanceAmount"] = x.BalanceAmount, ["crdr"] = x.IsCredit ? "Credit" : "Debit",
                            ["corporateName"] = x.CorporateName ?? "",
                        }));
                }
                var r = await GetWardWiseOutstandingAsync(ipNumber, wardId, corporateId, patientCategoryId, departmentId, isCredit, ct);
                return Flatten("Ward-wise Outstanding",
                    [("wardName", "Ward"), ("billCount", "Bills"), ("balanceAmount", "Balance")],
                    r.Wards.Select(x => new Dictionary<string, object?>
                    { ["wardName"] = x.WardName, ["billCount"] = x.BillCount, ["balanceAmount"] = x.BalanceAmount }));
            }
            case "day-book":
            {
                var d = date ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
                var r = await GetDayBookReportAsync(d, billType, billedByUserId, corporateId, ct);
                return Flatten("Day Book Report",
                    [("kind", "Kind"), ("reference", "Reference"), ("patientName", "Patient"), ("detail", "Detail"), ("amount", "Amount")],
                    r.BillsGenerated.Select(x => new Dictionary<string, object?>
                        { ["kind"] = "Bill", ["reference"] = x.BillNumber, ["patientName"] = x.PatientName, ["detail"] = "", ["amount"] = x.TotalAmount })
                    .Concat(r.PaymentsCollected.Select(x => new Dictionary<string, object?>
                        { ["kind"] = "Payment", ["reference"] = x.ReceiptNumber, ["patientName"] = x.PatientName, ["detail"] = x.PaymentModeName, ["amount"] = x.Amount })));
            }
            case "cancelled-bills":
            {
                var r = await GetCancelledBillsReportAsync(range, ct);
                return Flatten("Bill Cancelled Report",
                    [("billNumber", "Bill No"), ("billDate", "Date"), ("patientName", "Patient"), ("totalAmount", "Amount"), ("cancelReason", "Reason")],
                    r.Bills.Select(x => new Dictionary<string, object?>
                    {
                        ["billNumber"] = x.BillNumber, ["billDate"] = x.BillDate.ToString("yyyy-MM-dd"),
                        ["patientName"] = x.PatientName, ["totalAmount"] = x.TotalAmount, ["cancelReason"] = x.CancelReason ?? "",
                    }));
            }
            case "concession":
            {
                var branchId = RequireBranch();
                var allowedTypes = await GetAllowedBillTypesAsync(ct);
                var rows = await ToConcessionRowsAsync(
                    BuildConcessionQuery(branchId, allowedTypes, range.FromDate, range.ToDate).OrderByDescending(a => a.DecidedAt), ct);
                return Flatten("Concession Report",
                    [("billNumber", "Bill No"), ("billDate", "Date"), ("regNo", "Reg No"), ("patientName", "Patient"), ("age", "Age"),
                     ("gender", "Gender"), ("doctorName", "Doctor"), ("specialtyName", "Dept"), ("billAmount", "Bill Amount"),
                     ("requestedPercent", "Percent"), ("requestedAmount", "Amount"), ("reason", "Reason"),
                     ("requestedByName", "Requested By"), ("requestedAt", "Requested At"), ("approvedByName", "Approved By"), ("approvedAt", "Approved At")],
                    rows.Select(x => new Dictionary<string, object?>
                    {
                        ["billNumber"] = x.BillNumber, ["billDate"] = x.BillDate.ToString("yyyy-MM-dd"), ["regNo"] = x.RegNo,
                        ["patientName"] = x.PatientName, ["age"] = x.Age, ["gender"] = x.Gender.ToString(),
                        ["doctorName"] = x.DoctorName ?? "", ["specialtyName"] = x.SpecialtyName ?? "", ["billAmount"] = x.BillAmount,
                        ["requestedPercent"] = x.RequestedPercent, ["requestedAmount"] = x.RequestedAmount, ["reason"] = x.Reason ?? "",
                        ["requestedByName"] = x.RequestedByName, ["requestedAt"] = x.RequestedAt.ToString("yyyy-MM-dd HH:mm"),
                        ["approvedByName"] = x.ApprovedByName ?? "", ["approvedAt"] = x.ApprovedAt?.ToString("yyyy-MM-dd HH:mm") ?? "",
                    }));
            }
            case "refund":
            {
                var r = await GetRefundReportAsync(range, billType, corporateId, ct);
                return Flatten("Refund Report",
                    [("regNo", "Register No"), ("billNumber", "Bill No"), ("advanceRef", "Advance"), ("patientName", "Patient"),
                     ("voucherNo", "Voucher No"), ("amount", "Amount"), ("status", "Status"), ("reason", "Reason")],
                    r.Refunds.Select(x => new Dictionary<string, object?>
                    {
                        ["regNo"] = x.RegNo, ["billNumber"] = x.BillNumber ?? "", ["advanceRef"] = x.AdvanceId is { } aid ? $"#{aid}" : "",
                        ["patientName"] = x.PatientName, ["voucherNo"] = x.VoucherNo ?? "",
                        ["amount"] = x.Amount, ["status"] = x.Status, ["reason"] = x.Reason,
                    }));
            }
            case "advance":
            {
                var r = await GetAdvanceReportAsync(range, ct);
                return Flatten("Advance Report",
                    [("patientName", "Patient"), ("paymentModeName", "Mode"), ("amount", "Amount"), ("balanceAmount", "Balance")],
                    r.Advances.Select(x => new Dictionary<string, object?>
                    { ["patientName"] = x.PatientName, ["paymentModeName"] = x.PaymentModeName, ["amount"] = x.Amount, ["balanceAmount"] = x.BalanceAmount }));
            }
            case "tariff-list":
            {
                var r = await GetTariffListReportAsync(ct);
                return Flatten("Tariff List Report",
                    [("tariffName", "Tariff"), ("patientCategoryName", "Category"), ("serviceName", "Service"), ("rate", "Rate")],
                    r.Rows.Select(x => new Dictionary<string, object?>
                    { ["tariffName"] = x.TariffName, ["patientCategoryName"] = x.PatientCategoryName ?? "All", ["serviceName"] = x.ServiceName, ["rate"] = x.Rate }));
            }
            case "doctors-payout":
            {
                var r = await GetDoctorsPayoutReportAsync(range, ct);
                return Flatten("Doctors Payout Report",
                    [("doctorName", "Doctor"), ("serviceCategoryName", "Service Category"), ("billedAmount", "Billed"), ("payoutAmount", "Payout")],
                    r.Rows.Select(x => new Dictionary<string, object?>
                    { ["doctorName"] = x.DoctorName, ["serviceCategoryName"] = x.ServiceCategoryName, ["billedAmount"] = x.BilledAmount, ["payoutAmount"] = x.PayoutAmount }));
            }
            case "user-settlement":
            {
                var r = await GetUserSettlementReportAsync(range, ct);
                return Flatten("User Settlement Denomination Report",
                    [("userName", "Cashier"), ("paymentModeName", "Mode"), ("systemAmount", "System"), ("declaredAmount", "Declared"), ("variance", "Variance")],
                    r.Rows.Select(x => new Dictionary<string, object?>
                    {
                        ["userName"] = x.UserName, ["paymentModeName"] = x.PaymentModeName, ["systemAmount"] = x.SystemAmount,
                        ["declaredAmount"] = x.DeclaredAmount, ["variance"] = x.Variance,
                    }));
            }
            case "service-category-breakup":
            {
                var r = await GetServiceCategoryBreakupReportAsync(range, ct);
                return Flatten("Service-wise Breakup Report",
                    [("serviceCategoryName", "Service Category"), ("lineCount", "Lines"), ("amount", "Amount")],
                    r.Rows.Select(x => new Dictionary<string, object?>
                    { ["serviceCategoryName"] = x.ServiceCategoryName, ["lineCount"] = x.LineCount, ["amount"] = x.Amount }));
            }
            case "ip-service-billing-detail":
            {
                var r = await GetIpServiceBillingDetailReportAsync(range, ct);
                return Flatten("IP Service Billing Detail Report",
                    [("billNumber", "Bill No"), ("patientName", "Patient"), ("description", "Description"), ("quantity", "Qty"), ("rate", "Rate"), ("totalAmount", "Total")],
                    r.Rows.Select(x => new Dictionary<string, object?>
                    {
                        ["billNumber"] = x.BillNumber, ["patientName"] = x.PatientName, ["description"] = x.Description,
                        ["quantity"] = x.Quantity, ["rate"] = x.Rate, ["totalAmount"] = x.TotalAmount,
                    }));
            }
            case "revenue":
            {
                var r = await GetRevenueReportAsync(range, reportType, billType, corporateId, doctorId, serviceId, serviceCategoryId, departmentId, isCredit, ct);
                return r.IsSummary
                    ? Flatten("Revenue Report (Summary)",
                        [("departmentName", "Department"), ("billCount", "Bills"), ("amount", "Amount")],
                        r.Rows.Select(x => new Dictionary<string, object?>
                        { ["departmentName"] = x.DepartmentName, ["billCount"] = x.BillCount, ["amount"] = x.Amount }))
                    : Flatten("Revenue Report (Detail)",
                        [("billNumber", "Bill No"), ("billDate", "Date"), ("patientName", "Patient"), ("doctorName", "Doctor"),
                         ("departmentName", "Department"), ("serviceCategoryName", "Header"), ("serviceName", "Service"),
                         ("amount", "Amount"), ("isCredit", "Paid/Credit")],
                        r.Rows.Select(x => new Dictionary<string, object?>
                        {
                            ["billNumber"] = x.BillNumber, ["billDate"] = x.BillDate?.ToString("yyyy-MM-dd"), ["patientName"] = x.PatientName,
                            ["doctorName"] = x.DoctorName ?? "", ["departmentName"] = x.DepartmentName ?? "",
                            ["serviceCategoryName"] = x.ServiceCategoryName ?? "", ["serviceName"] = x.ServiceName,
                            ["amount"] = x.Amount, ["isCredit"] = x.IsCredit == true ? "Credit" : "Paid",
                        }));
            }
            case "outstanding":
            {
                var r = await GetOutstandingReportAsync(range, reportType, billType, billedByUserId, corporateId, ct);
                return r.IsSummary
                    ? Flatten("Outstanding Report (Summary)",
                        [("billTypeName", "Bill Type"), ("billCount", "Bills"), ("balanceAmount", "Balance")],
                        r.Rows.Select(x => new Dictionary<string, object?>
                        { ["billTypeName"] = x.BillTypeName, ["billCount"] = x.BillCount, ["balanceAmount"] = x.BalanceAmount }))
                    : Flatten("Outstanding Report (Detail)",
                        [("regNo", "Reg No"), ("billNumber", "Bill No"), ("billDate", "Date"), ("patientName", "Patient"),
                         ("billTypeName", "Bill Type"), ("corporateName", "Company"), ("totalAmount", "Total"), ("paidAmount", "Paid"),
                         ("balanceAmount", "Balance"), ("crdr", "CR/DR")],
                        r.Rows.Select(x => new Dictionary<string, object?>
                        {
                            ["regNo"] = x.RegNo, ["billNumber"] = x.BillNumber, ["billDate"] = x.BillDate?.ToString("yyyy-MM-dd"),
                            ["patientName"] = x.PatientName, ["billTypeName"] = x.BillTypeName, ["corporateName"] = x.CorporateName ?? "",
                            ["totalAmount"] = x.TotalAmount, ["paidAmount"] = x.PaidAmount, ["balanceAmount"] = x.BalanceAmount,
                            ["crdr"] = x.IsCredit == true ? "Credit" : "Debit",
                        }));
            }
            default:
                throw new DomainException($"Unknown billing report '{reportKey}'.");
        }
    }

    private static ReportResultDto Flatten(
        string name, IReadOnlyList<(string Key, string Label)> columns, IEnumerable<Dictionary<string, object?>> rows) =>
        new(name, columns.Select(c => new ReportColumnDto(c.Key, c.Label)).ToList(),
            rows.Select(r => (IReadOnlyDictionary<string, object?>)r).ToList());

    private async Task<IReadOnlySet<BillType>> GetAllowedBillTypesAsync(CancellationToken ct)
    {
        var granted = await permissions.GetUserPermissionsAsync(currentUser.UserId, ct);
        var allowed = new HashSet<BillType>();
        if (granted.Contains(PermissionRegistry.BillViewOp)) allowed.Add(BillType.Op);
        if (granted.Contains(PermissionRegistry.BillViewIp)) allowed.Add(BillType.Ip);
        if (granted.Contains(PermissionRegistry.BillViewEmergency)) allowed.Add(BillType.Emergency);
        return allowed;
    }

    private async Task RequireIpAccessAsync(CancellationToken ct)
    {
        if (!(await GetAllowedBillTypesAsync(ct)).Contains(BillType.Ip))
        {
            throw new ForbiddenException("You don't have permission to view IP bills.");
        }
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
