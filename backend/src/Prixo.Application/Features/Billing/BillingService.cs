using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Auth;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Application.Features.Ipd;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Emr;
using Prixo.Domain.Entities.Er;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Mrd;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Billing;

/// <summary>
/// OP billing end to end: generate a bill from a visit, price lines via the active
/// Tariff, run the discount threshold-approval workflow, collect payments and refunds.
/// IP billing (BillType.Ip, Phase 9) reuses every bit of this — GenerateIpBillAsync and
/// RefreshBedChargesAsync are the only IP-specific additions; discounts/payments/refunds/
/// cancellation are the exact same code path as an OP bill.
/// </summary>
public class BillingService(
    IAppDbContext db,
    IUnitOfWork unitOfWork,
    ISequenceService sequences,
    ICurrentUser currentUser,
    IActivityLogger activity,
    IPdfService pdf,
    IFinanceLedgerPoster ledgerPoster,
    PermissionService permissions,
    TimeProvider timeProvider)
{
    public async Task<BillDetailDto> GenerateOpBillAsync(GenerateOpBillRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var visit = await db.OpVisits.Include(v => v.Patient).Include(v => v.Doctor).ThenInclude(d => d!.User)
            .Include(v => v.MhcDoctor)
            .FirstOrDefaultAsync(v => v.Id == request.OpVisitId, ct)
            ?? throw new NotFoundException(nameof(OpVisit), request.OpVisitId);

        if (visit.Status == OpVisitStatus.Cancelled)
        {
            throw new DomainException("This visit has been cancelled and cannot be billed.");
        }

        if (await db.Bills.AnyAsync(b => b.OpVisitId == request.OpVisitId && b.Status != BillStatus.Cancelled, ct))
        {
            throw new DomainException("A bill already exists for this visit.");
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        await DayCloseGuard.EnsureOpenAsync(db, branchId, today, ct);

        var year = today.Year;
        var billNumberValue = await sequences.NextValueAsync(SequenceKeys.BillNumber, branchId, year, ct);
        var bill = new Bill
        {
            BranchId = branchId,
            PatientId = visit.PatientId,
            Patient = visit.Patient,
            OpVisitId = visit.Id,
            BillNumber = $"BILL-{year}-{billNumberValue.ToString().PadLeft(6, '0')}",
            BillDate = today,
            BillType = BillType.Op,
            Status = BillStatus.Open,
        };

        // Category == Opd visits carry DoctorId (a regular Doctor); Category == Mhc visits carry
        // MhcDoctorId instead (a separate panel-doctor tagging list, not User-linked) — see
        // OpVisit's own doc comments. Falling back between the two avoids a NullReferenceException
        // on MHC bill generation, which visit.Doctor!.User!.FullName would otherwise throw.
        var consultantName = visit.Doctor?.User?.FullName ?? visit.MhcDoctor?.Name ?? "-";
        bill.Items.Add(new BillItem
        {
            SourceType = BillItemSourceType.Consultation,
            Description = $"Consultation — {consultantName} ({visit.VisitType})",
            Quantity = 1,
            Rate = visit.ConsultationFee,
        });

        var linkedOrderIds = await db.LabOrders.AsNoTracking().Where(o => o.OpVisitId == visit.Id && o.ClinicalOrderId != null)
            .Select(o => o.ClinicalOrderId!.Value)
            .Concat(db.RadOrders.AsNoTracking().Where(o => o.OpVisitId == visit.Id && o.ClinicalOrderId != null).Select(o => o.ClinicalOrderId!.Value))
            .ToListAsync(ct);
        var orders = await db.ClinicalOrders.AsNoTracking()
            .Where(o => o.VisitId == visit.Id && o.Status != ClinicalOrderStatus.Cancelled && !linkedOrderIds.Contains(o.Id))
            .ToListAsync(ct);
        foreach (var order in orders)
        {
            bill.Items.Add(new BillItem
            {
                SourceType = BillItemSourceType.ClinicalOrder,
                ClinicalOrderId = order.Id,
                Description = order.Description,
                Quantity = 1,
                Rate = 0,
            });
        }

        foreach (var item in bill.Items)
        {
            BillMath.ComputeItemAmounts(item);
        }
        BillMath.RecomputeBillTotals(bill);

        db.Bills.Add(bill);
        await db.SaveChangesAsync(ct);
        await PostPendingDiagnosticsAsync(bill, ct);
        return await GetByIdAsync(bill.Id, ct);
    }

    /// <summary>
    /// OP Billing's own patient search (legacy "OP Bill Payment" search) — lists OP/MHC visits
    /// for a cashier to click into, separate from Patient Registration's patient search since it
    /// searches visits (encounter-scoped) rather than patient master records, and surfaces
    /// whether a bill already exists so the frontend can offer "Open" instead of "Generate".
    /// Branch scoping comes from OpVisit's own global query filter, same as every other OpVisit query.
    /// </summary>
    public async Task<PagedResult<OpBillingSearchResultDto>> SearchOpVisitsForBillingAsync(
        OpBillingSearchQuery query, CancellationToken ct = default)
    {
        var visits = db.OpVisits.AsNoTracking()
            .Include(v => v.Patient).ThenInclude(p => p!.PatientCategory)
            .Include(v => v.Patient).ThenInclude(p => p!.Referrer)
            .Include(v => v.Doctor).ThenInclude(d => d!.User)
            .Include(v => v.Doctor).ThenInclude(d => d!.Specialty)
            .Include(v => v.MhcDoctor)
            .AsQueryable();

        if (query.FromDate is { } from) visits = visits.Where(v => v.VisitDate >= from);
        if (query.ToDate is { } to) visits = visits.Where(v => v.VisitDate <= to);
        if (!string.IsNullOrWhiteSpace(query.PatientName))
        {
            var term = $"%{query.PatientName.Trim()}%";
            visits = visits.Where(v => EF.Functions.ILike(v.Patient!.FirstName, term) || EF.Functions.ILike(v.Patient!.LastName, term));
        }
        if (!string.IsNullOrWhiteSpace(query.RegNo))
        {
            visits = visits.Where(v => EF.Functions.ILike(v.Patient!.Uhid, $"%{query.RegNo.Trim()}%"));
        }
        if (!string.IsNullOrWhiteSpace(query.Mobile))
        {
            visits = visits.Where(v => v.Patient!.Mobile.Contains(query.Mobile.Trim()));
        }
        if (query.Gender is { } gender) visits = visits.Where(v => v.Patient!.Gender == gender);
        if (!string.IsNullOrWhiteSpace(query.DoctorName))
        {
            var term = $"%{query.DoctorName.Trim()}%";
            visits = visits.Where(v => v.Doctor != null && EF.Functions.ILike(v.Doctor.User!.FullName, term));
        }
        if (!string.IsNullOrWhiteSpace(query.IpNumber))
        {
            var term = $"%{query.IpNumber.Trim()}%";
            visits = visits.Where(v => db.Admissions.Any(a => a.PatientId == v.PatientId && EF.Functions.ILike(a.AdmissionNumber, term)));
        }
        if (query.MrdOnly)
        {
            visits = visits.Where(v => db.RecordFiles.Any(f => f.Admission!.PatientId == v.PatientId));
        }
        if (query.EmrOnly)
        {
            visits = visits.Where(v => db.ClinicalNotes.Any(n => n.PatientId == v.PatientId));
        }
        if (query.PackageOnly)
        {
            visits = visits.Where(v => v.Category == VisitCategory.Mhc);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            visits = visits.Where(v =>
                EF.Functions.ILike(v.Patient!.Uhid, term) ||
                EF.Functions.ILike(v.Patient!.FirstName, term) ||
                EF.Functions.ILike(v.Patient!.LastName, term) ||
                EF.Functions.ILike(v.VisitNumber, term));
        }

        visits = visits.OrderByDescending(v => v.VisitDate).ThenByDescending(v => v.Id);
        var totalCount = await visits.CountAsync(ct);
        var pageVisits = await visits.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);

        var visitIds = pageVisits.Select(v => v.Id).ToList();
        var bills = await db.Bills.AsNoTracking()
            .Where(b => b.OpVisitId != null && visitIds.Contains(b.OpVisitId!.Value) && b.Status != BillStatus.Cancelled)
            .Select(b => new { OpVisitId = b.OpVisitId!.Value, b.Id, b.BillNumber, b.Status, CorporateName = b.Corporate != null ? b.Corporate.Name : null })
            .ToListAsync(ct);
        var billsByVisit = bills.ToDictionary(b => b.OpVisitId);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var items = pageVisits.Select(v =>
        {
            billsByVisit.TryGetValue(v.Id, out var bill);
            var doctorName = v.Doctor?.User?.FullName ?? v.MhcDoctor?.Name;
            return new OpBillingSearchResultDto(
                v.Id, v.PatientId, v.Patient!.Uhid, $"{v.Patient.FirstName} {v.Patient.LastName}", v.VisitDate,
                IpdMath.FormatAge(v.Patient.DateOfBirth, v.Patient.EstimatedAge, today), v.Patient.Gender,
                v.Patient.Mobile, doctorName, v.Doctor?.Specialty?.Name, v.Patient.PatientCategory?.Name,
                v.Patient.Referrer?.Name, bill?.Id, bill?.BillNumber, bill?.Status, bill?.CorporateName);
        }).ToList();

        return PagedResult<OpBillingSearchResultDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    /// <summary>Creates an IP bill against an admission and computes its first bed-charge line. Everything else (Service/Package lines, discounts, payments) reuses the OP machinery.</summary>
    public async Task<BillDetailDto> GenerateIpBillAsync(long admissionId, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var admission = await db.Admissions.Include(a => a.Patient).FirstOrDefaultAsync(a => a.Id == admissionId, ct)
            ?? throw new NotFoundException(nameof(Admission), admissionId);

        var existing = await db.Bills.Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.AdmissionId == admissionId && b.Status != BillStatus.Cancelled, ct);
        if (existing is not null)
        {
            // One running IP bill per admission (it may already exist because pharmacy/OT posted a
            // charge first): bring it up to date and return it instead of failing.
            if (existing.Status is not (BillStatus.Paid or BillStatus.Parked))
            {
                await RefreshBedChargesInternalAsync(existing, admissionId, ct);
            }
            if (existing.Status != BillStatus.Parked)
            {
                await PostPendingDiagnosticsAsync(existing, ct);
            }
            return await GetByIdAsync(existing.Id, ct);
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        await DayCloseGuard.EnsureOpenAsync(db, branchId, today, ct);

        var year = today.Year;
        var billNumberValue = await sequences.NextValueAsync(SequenceKeys.BillNumber, branchId, year, ct);
        var bill = new Bill
        {
            BranchId = branchId,
            PatientId = admission.PatientId,
            Patient = admission.Patient,
            AdmissionId = admission.Id,
            BillNumber = $"BILL-{year}-{billNumberValue.ToString().PadLeft(6, '0')}",
            BillDate = today,
            BillType = BillType.Ip,
            Status = BillStatus.Open,
        };
        db.Bills.Add(bill);
        await db.SaveChangesAsync(ct);

        await RefreshBedChargesInternalAsync(bill, admissionId, ct);
        await PostPendingDiagnosticsAsync(bill, ct);
        return await GetByIdAsync(bill.Id, ct);
    }

    /// <summary>Creates an Emergency bill empty — "post-facto friendly" (plan §16): nothing is auto-priced, staff add Service/Package lines afterward via the same AddManualItemAsync every other bill type uses.</summary>
    public async Task<BillDetailDto> GenerateEmergencyBillAsync(long emergencyVisitId, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var visit = await db.EmergencyVisits.Include(v => v.Patient).FirstOrDefaultAsync(v => v.Id == emergencyVisitId, ct)
            ?? throw new NotFoundException(nameof(EmergencyVisit), emergencyVisitId);

        if (await db.Bills.AnyAsync(b => b.EmergencyVisitId == emergencyVisitId && b.Status != BillStatus.Cancelled, ct))
        {
            throw new DomainException("A bill already exists for this ER visit.");
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        await DayCloseGuard.EnsureOpenAsync(db, branchId, today, ct);

        var year = today.Year;
        var billNumberValue = await sequences.NextValueAsync(SequenceKeys.BillNumber, branchId, year, ct);
        var bill = new Bill
        {
            BranchId = branchId,
            PatientId = visit.PatientId,
            Patient = visit.Patient,
            EmergencyVisitId = visit.Id,
            BillNumber = $"BILL-{year}-{billNumberValue.ToString().PadLeft(6, '0')}",
            BillDate = today,
            BillType = BillType.Emergency,
            Status = BillStatus.Open,
        };
        db.Bills.Add(bill);
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(bill.Id, ct);
    }

    /// <summary>
    /// Recomputes the bill's BedCharge lines wholesale from BedAllocation history — one line
    /// per allocation segment, quantity in whole days (IpdMath.ComputeBedDays) at that
    /// segment's snapshotted BedChargePerDay. Idempotent; call again any time (e.g. right
    /// before discharge) to bring charges up to date. Also runs from IpBedChargeRefreshJob
    /// (daily Hangfire) so a running IP bill's charges stay current without a manual click.
    /// </summary>
    public async Task<BillDetailDto> RefreshBedChargesAsync(long billId, CancellationToken ct = default)
    {
        var bill = await LoadEditableBillAsync(billId, ct);
        if (bill.AdmissionId is not { } admissionId)
        {
            throw new DomainException("Only an IP bill linked to an admission can have bed charges refreshed.");
        }
        await RefreshBedChargesInternalAsync(bill, admissionId, ct);
        await PostPendingDiagnosticsAsync(bill, ct);
        return await GetByIdAsync(bill.Id, ct);
    }

    /// <summary>
    /// IpBedChargeRefreshJob's entry point (Cron.Daily): recomputes bed-charge lines for every
    /// still-open IP bill and, via RefreshBedChargesInternalAsync's AutoDrawAdvancesAsync step,
    /// sweeps admission deposits against the recomputed balance. Runs with no HTTP context, so
    /// the per-user checks a live request goes through (RequireBranch, EnsureCanAccessBillTypeAsync)
    /// are skipped by design — same posture as NoShowJob. Each bill is its own transaction so one
    /// admission's bad state can't block the rest; RefreshBedChargesInternalAsync is idempotent,
    /// so a retried job re-running an already-refreshed bill is harmless.
    /// </summary>
    public async Task<int> RefreshAllOpenIpBedChargesAsync(CancellationToken ct = default)
    {
        var openIpBills = await db.Bills.AsNoTracking()
            .Where(b => b.BillType == BillType.Ip && b.AdmissionId != null &&
                b.Status != BillStatus.Cancelled && b.Status != BillStatus.Paid && b.Status != BillStatus.Parked)
            .Select(b => new { b.Id, AdmissionId = b.AdmissionId!.Value })
            .ToListAsync(ct);

        foreach (var row in openIpBills)
        {
            await unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                var bill = await db.Bills.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == row.Id, token)
                    ?? throw new NotFoundException(nameof(Bill), row.Id);
                await RefreshBedChargesInternalAsync(bill, row.AdmissionId, token);
            }, ct);
        }

        if (openIpBills.Count > 0)
        {
            await activity.LogAsync(ActivityEvents.BedChargesAutoRefreshed,
                $"{openIpBills.Count} open IP bill(s) had bed charges auto-refreshed", saveImmediately: true, ct: ct);
        }
        return openIpBills.Count;
    }

    private async Task RefreshBedChargesInternalAsync(Bill bill, long admissionId, CancellationToken ct)
    {
        var allocations = await db.BedAllocations.AsNoTracking()
            .Where(ba => ba.AdmissionId == admissionId)
            .Include(ba => ba.Bed).ThenInclude(b => b!.Room).ThenInclude(r => r!.Ward)
            .OrderBy(ba => ba.AllocatedAt)
            .ToListAsync(ct);

        foreach (var old in bill.Items.Where(i => i.SourceType == BillItemSourceType.BedCharge).ToList())
        {
            bill.Items.Remove(old);
            db.BillItems.Remove(old);
        }

        var now = timeProvider.GetUtcNow();
        foreach (var allocation in allocations)
        {
            var days = IpdMath.ComputeBedDays(allocation.AllocatedAt, allocation.ReleasedAt ?? now);
            var item = new BillItem
            {
                SourceType = BillItemSourceType.BedCharge,
                Description = $"Bed charge — {allocation.Bed!.Room!.Ward!.Name}/{allocation.Bed.Room.Name}/{allocation.Bed.Code}",
                Quantity = days,
                Rate = allocation.BedChargePerDay,
            };
            BillMath.ComputeItemAmounts(item);
            bill.Items.Add(item);
        }
        BillMath.RecomputeBillTotals(bill);
        await AutoDrawAdvancesAsync(bill, admissionId, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Auto-sweeps the admission's available (undrawn) advance balance against this bill's
    /// outstanding balance, closing the previously-documented gap "deposits stay separately
    /// visible/collectible, not yet consumed by billing." Runs FIFO by ReceivedAt so the oldest
    /// deposit is consumed first; DrawAdvanceAsync remains available for a manual, out-of-order
    /// draw against a specific advance. Reuses the exact PaidAmount/BalanceAmount/Status
    /// recompute CollectPaymentAsync/DrawAdvanceAsync already do — no cash moves, so no
    /// BillPayment/receipt is written. Runs every time bed charges are refreshed: on IP bill
    /// generation (sweeping the admission deposit immediately), on a manual refresh, and from
    /// the nightly IpBedChargeRefreshJob.
    /// </summary>
    private async Task AutoDrawAdvancesAsync(Bill bill, long admissionId, CancellationToken ct)
    {
        if (bill.BalanceAmount <= 0)
        {
            return;
        }

        var advances = await db.Advances
            .Where(a => a.AdmissionId == admissionId && a.BalanceAmount > 0)
            .OrderBy(a => a.ReceivedAt)
            .ToListAsync(ct);

        var draws = BillMath.PlanAdvanceSweep(
            advances.Select(a => new AdvanceBalance(a.Id, a.BalanceAmount)), bill.BalanceAmount);
        if (draws.Count == 0)
        {
            return;
        }

        var advancesById = advances.ToDictionary(a => a.Id);
        var totalDrawn = 0m;
        foreach (var draw in draws)
        {
            advancesById[draw.AdvanceId].BalanceAmount -= draw.Amount;
            bill.PaidAmount += draw.Amount;
            totalDrawn += draw.Amount;
        }
        bill.BalanceAmount = Math.Max(bill.TotalAmount - bill.PaidAmount, 0);
        bill.Status = bill.BalanceAmount <= 0 ? BillStatus.Paid : BillStatus.PartiallyPaid;

        await activity.LogAsync(ActivityEvents.AdvanceAutoDrawn,
            $"{totalDrawn:0.00} auto-drawn from admission deposits against bill {bill.BillNumber}", ct: ct);
    }

    public async Task<PagedResult<BillListItemDto>> GetPagedAsync(BillQuery query, CancellationToken ct = default)
    {
        var bills = await FilterBillsAsync(query, ct);

        var totalCount = await bills.CountAsync(ct);
        var items = await bills
            .OrderByDescending(b => b.BillDate).ThenByDescending(b => b.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ToListItemSelector())
            .ToListAsync(ct);

        return PagedResult<BillListItemDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    /// <summary>Same filters as GetPagedAsync but unpaged — for the "OP/IP Bill Report" CSV/Excel/PDF export.</summary>
    public async Task<IReadOnlyList<BillListItemDto>> GetForReportAsync(BillQuery query, CancellationToken ct = default) =>
        await (await FilterBillsAsync(query, ct))
            .OrderByDescending(b => b.BillDate).ThenByDescending(b => b.Id)
            .Select(ToListItemSelector())
            .ToListAsync(ct);

    /// <summary>
    /// RBAC bill-type scoping: narrows every Bills query to the caller's permitted
    /// BillViewOp/Ip/Emergency set. An explicit BillType filter outside that set is a 403
    /// (the caller asked for something they can't see); no filter at all silently narrows to
    /// what they're allowed rather than erroring, so an OP-only cashier's plain bill search
    /// just never surfaces IP bills.
    /// </summary>
    private async Task<IQueryable<Bill>> FilterBillsAsync(BillQuery query, CancellationToken ct)
    {
        var allowedTypes = await GetAllowedBillTypesAsync(ct);
        var bills = db.Bills.AsNoTracking();

        if (query.BillType is { } billType)
        {
            if (!allowedTypes.Contains(billType))
            {
                throw new ForbiddenException($"You don't have permission to view {billType} bills.");
            }
            bills = bills.Where(b => b.BillType == billType);
        }
        else
        {
            bills = bills.Where(b => allowedTypes.Contains(b.BillType));
        }

        if (query.PatientId is { } patientId) bills = bills.Where(b => b.PatientId == patientId);
        if (query.FromDate is { } from) bills = bills.Where(b => b.BillDate >= from);
        if (query.ToDate is { } to) bills = bills.Where(b => b.BillDate <= to);
        if (query.Status is { } status) bills = bills.Where(b => b.Status == status);
        if (query.CorporateId is { } corporateId) bills = bills.Where(b => b.CorporateId == corporateId);
        if (query.BilledByUserId is { } billedByUserId) bills = bills.Where(b => b.CreatedBy == billedByUserId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            bills = bills.Where(b =>
                EF.Functions.ILike(b.BillNumber, term) ||
                EF.Functions.ILike(b.Patient!.Uhid, term) ||
                EF.Functions.ILike(b.Patient!.FirstName, term) ||
                EF.Functions.ILike(b.Patient!.LastName, term));
        }
        return bills;
    }

    private async Task<IReadOnlySet<BillType>> GetAllowedBillTypesAsync(CancellationToken ct)
    {
        var granted = await permissions.GetUserPermissionsAsync(currentUser.UserId, ct);
        var allowed = new HashSet<BillType>();
        if (granted.Contains(PermissionRegistry.BillViewOp)) allowed.Add(BillType.Op);
        if (granted.Contains(PermissionRegistry.BillViewIp)) allowed.Add(BillType.Ip);
        if (granted.Contains(PermissionRegistry.BillViewEmergency)) allowed.Add(BillType.Emergency);
        return allowed;
    }

    private async Task EnsureCanAccessBillTypeAsync(BillType billType, CancellationToken ct)
    {
        if (!(await GetAllowedBillTypesAsync(ct)).Contains(billType))
        {
            throw new ForbiddenException($"You don't have permission to work with {billType} bills.");
        }
    }

    private static Expression<Func<Bill, BillListItemDto>> ToListItemSelector() => b => new BillListItemDto(
        b.Id, b.BillNumber, b.BillDate, b.PatientId, b.Patient!.Uhid,
        b.Patient!.FirstName + " " + b.Patient!.LastName, b.BillType, b.Status,
        b.TotalAmount, b.PaidAmount, b.BalanceAmount, b.IsCreditBill, b.AdmissionId);

    public async Task<BillDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var bill = await db.Bills.AsNoTracking()
            .Include(b => b.Patient)
            .Include(b => b.Corporate)
            .Include(b => b.Items).ThenInclude(i => i.Service)
            .Include(b => b.Items).ThenInclude(i => i.Package)
            .Include(b => b.Payments).ThenInclude(p => p.PaymentMode)
            .FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new NotFoundException(nameof(Bill), id);

        var allowedTypes = await GetAllowedBillTypesAsync(ct);
        if (!allowedTypes.Contains(bill.BillType))
        {
            throw new ForbiddenException($"You don't have permission to view {bill.BillType} bills.");
        }

        decimal? creditLimit = null;
        decimal? creditExposure = null;
        if (bill.AdmissionId is { } admissionId)
        {
            creditLimit = await db.Admissions.AsNoTracking().Where(a => a.Id == admissionId).Select(a => a.CreditLimit).FirstOrDefaultAsync(ct);
            if (creditLimit is not null)
            {
                var availableAdvances = await db.Advances.AsNoTracking()
                    .Where(a => a.AdmissionId == admissionId).SumAsync(a => a.BalanceAmount, ct);
                creditExposure = bill.BalanceAmount - availableAdvances;
            }
        }
        var insuranceStatus = await GetInsuranceStatusAsync(bill, ct);
        return ToDetailDto(bill, creditLimit, creditExposure, insuranceStatus);
    }

    /// <summary>
    /// A not-yet-resolved Claim against this exact bill outranks a not-yet-resolved
    /// PreAuthRequest against the same encounter — see BillInsuranceStatusDto's doc comment.
    /// Advisory only; never blocks CollectPaymentAsync.
    /// </summary>
    private async Task<BillInsuranceStatusDto?> GetInsuranceStatusAsync(Bill bill, CancellationToken ct)
    {
        var activeClaim = await db.Claims.AsNoTracking()
            .Where(c => c.BillId == bill.Id && c.Status != ClaimStatus.Settled && c.Status != ClaimStatus.Denied)
            .OrderByDescending(c => c.Id).FirstOrDefaultAsync(ct);
        if (activeClaim is not null)
        {
            return new BillInsuranceStatusDto(null, null, null, null, activeClaim.Id, activeClaim.ClaimNumber, activeClaim.Status);
        }

        if (bill.OpVisitId is null && bill.AdmissionId is null)
        {
            return null;
        }
        var activePreAuth = await db.PreAuthRequests.AsNoTracking()
            .Where(p => p.Status != PreAuthStatus.Rejected
                && ((bill.OpVisitId != null && p.OpVisitId == bill.OpVisitId) || (bill.AdmissionId != null && p.AdmissionId == bill.AdmissionId)))
            .OrderByDescending(p => p.Id).FirstOrDefaultAsync(ct);
        return activePreAuth is null
            ? null
            : new BillInsuranceStatusDto(activePreAuth.Id, activePreAuth.PreAuthNumber, activePreAuth.Status, activePreAuth.ApprovedAmount, null, null, null);
    }

    public async Task<long> AddManualItemAsync(long billId, AddManualBillItemRequest request, CancellationToken ct = default)
    {
        var bill = await LoadEditableBillAsync(billId, ct);

        string description;
        decimal rate;
        decimal taxPercent = 0;
        long? serviceId = null;
        long? packageId = null;

        switch (request.SourceType)
        {
            case BillItemSourceType.Service:
                var service = await db.Services.Include(s => s.Tax).FirstOrDefaultAsync(s => s.Id == request.ServiceId, ct)
                    ?? throw new NotFoundException(nameof(Service), request.ServiceId!.Value);
                var roomTypeId = bill.AdmissionId is { } admIdForRate
                    ? await db.Admissions.AsNoTracking().Where(a => a.Id == admIdForRate)
                        .Select(a => (long?)a.CurrentBed!.Room!.RoomTypeId).FirstOrDefaultAsync(ct)
                    : null;
                rate = await ResolveServiceRateAsync(bill.BranchId, service, bill.Patient?.PatientCategoryId, bill.BillDate, roomTypeId, ct);
                description = service.Name;
                taxPercent = service.IsTaxable ? (service.Tax?.RatePercent ?? 0) : 0;
                serviceId = service.Id;
                break;
            case BillItemSourceType.Package:
                var package = await db.Packages.FirstOrDefaultAsync(p => p.Id == request.PackageId, ct)
                    ?? throw new NotFoundException(nameof(Package), request.PackageId!.Value);
                rate = package.Price;
                description = package.Name;
                packageId = package.Id;
                break;
            default:
                throw new DomainException("Only Service or Package lines can be added manually.");
        }

        var item = new BillItem
        {
            BillId = bill.Id,
            SourceType = request.SourceType,
            ServiceId = serviceId,
            PackageId = packageId,
            Description = description,
            Quantity = request.Quantity,
            Rate = rate,
            TaxPercent = taxPercent,
        };
        BillMath.ComputeItemAmounts(item);
        bill.Items.Add(item);
        BillMath.RecomputeBillTotals(bill);
        await db.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task UpdateItemAsync(long billId, long itemId, UpdateBillItemRequest request, CancellationToken ct = default)
    {
        var bill = await LoadEditableBillAsync(billId, ct);
        var item = bill.Items.FirstOrDefault(i => i.Id == itemId) ?? throw new NotFoundException(nameof(BillItem), itemId);

        item.Description = request.Description;
        item.Quantity = request.Quantity;
        item.Rate = request.Rate;
        BillMath.ApplyItemDiscountPercent(item, item.DiscountPercent);
        BillMath.RecomputeBillTotals(bill);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveItemAsync(long billId, long itemId, CancellationToken ct = default)
    {
        var bill = await LoadEditableBillAsync(billId, ct);
        var item = bill.Items.FirstOrDefault(i => i.Id == itemId) ?? throw new NotFoundException(nameof(BillItem), itemId);

        bill.Items.Remove(item);
        db.BillItems.Remove(item);
        BillMath.RecomputeBillTotals(bill);
        await db.SaveChangesAsync(ct);
    }

    public async Task<DiscountApprovalDto?> ApplyItemDiscountAsync(
        long billId, long itemId, ApplyItemDiscountRequest request, CancellationToken ct = default)
    {
        var bill = await LoadEditableBillAsync(billId, ct);
        var item = bill.Items.FirstOrDefault(i => i.Id == itemId) ?? throw new NotFoundException(nameof(BillItem), itemId);

        var (percent, discountType) = await ValidateDiscountAsync(request.DiscountTypeId, request.Percent, ct);
        var requestedAmount = Math.Round(item.Rate * item.Quantity * percent / 100, 2);

        if (!BillMath.RequiresApproval(percent, discountType?.ApprovalThresholdPercent ?? 0))
        {
            BillMath.ApplyItemDiscountPercent(item, percent);
            BillMath.RecomputeBillTotals(bill);
            await db.SaveChangesAsync(ct);
            return null;
        }

        var approval = new DiscountApproval
        {
            BillId = bill.Id,
            BillItemId = item.Id,
            DiscountTypeId = request.DiscountTypeId,
            RequestedPercent = percent,
            RequestedAmount = requestedAmount,
            Reason = request.Reason,
        };
        db.DiscountApprovals.Add(approval);
        await activity.LogAsync(ActivityEvents.DiscountRequested,
            $"Item discount of {percent}% requested on bill {bill.BillNumber} (line: {item.Description})", ct: ct);
        await db.SaveChangesAsync(ct);
        return ToDiscountApprovalDto(approval, bill.BillNumber, item.Description);
    }

    public async Task<DiscountApprovalDto?> ApplyBillDiscountAsync(
        long billId, ApplyBillDiscountRequest request, CancellationToken ct = default)
    {
        var bill = await LoadEditableBillAsync(billId, ct);
        var (percent, discountType) = await ValidateDiscountAsync(request.DiscountTypeId, request.Percent, ct);
        var itemDiscountTotal = bill.Items.Sum(i => i.DiscountAmount);
        var baseAfterItemDiscount = bill.SubTotal - itemDiscountTotal;
        var requestedAmount = Math.Round(baseAfterItemDiscount * percent / 100, 2);

        if (!BillMath.RequiresApproval(percent, discountType?.ApprovalThresholdPercent ?? 0))
        {
            bill.BillDiscountPercent = percent;
            BillMath.RecomputeBillTotals(bill);
            await db.SaveChangesAsync(ct);
            return null;
        }

        var approval = new DiscountApproval
        {
            BillId = bill.Id,
            BillItemId = null,
            DiscountTypeId = request.DiscountTypeId,
            RequestedPercent = percent,
            RequestedAmount = requestedAmount,
            Reason = request.Reason,
        };
        db.DiscountApprovals.Add(approval);
        await activity.LogAsync(ActivityEvents.DiscountRequested,
            $"Bill-level discount of {percent}% requested on bill {bill.BillNumber}", ct: ct);
        await db.SaveChangesAsync(ct);
        return ToDiscountApprovalDto(approval, bill.BillNumber, null);
    }

    public async Task<IReadOnlyList<DiscountApprovalDto>> GetPendingDiscountApprovalsAsync(CancellationToken ct = default)
    {
        var allowedTypes = await GetAllowedBillTypesAsync(ct);
        return await db.DiscountApprovals.AsNoTracking()
            .Where(a => a.Status == DiscountApprovalStatus.Pending && allowedTypes.Contains(a.Bill!.BillType))
            .Include(a => a.Bill)
            .Include(a => a.BillItem)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new DiscountApprovalDto(
                a.Id, a.BillId, a.Bill!.BillNumber, a.BillItemId, a.BillItem != null ? a.BillItem.Description : null,
                a.RequestedPercent, a.RequestedAmount, a.Reason, a.Status,
                a.DecidedByUserId, null, a.DecidedAt, a.RejectionReason, a.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task DecideDiscountAsync(long approvalId, DecideDiscountRequest request, CancellationToken ct = default)
    {
        var approval = await db.DiscountApprovals.Include(a => a.Bill).ThenInclude(b => b!.Items)
            .FirstOrDefaultAsync(a => a.Id == approvalId, ct)
            ?? throw new NotFoundException(nameof(DiscountApproval), approvalId);
        if (approval.Status != DiscountApprovalStatus.Pending)
        {
            throw new DomainException($"A discount request that is {approval.Status} cannot be decided again.");
        }

        var bill = approval.Bill!;
        await EnsureCanAccessBillTypeAsync(bill.BillType, ct);
        if (request.Approve)
        {
            if (approval.BillItemId is { } itemId)
            {
                var item = bill.Items.FirstOrDefault(i => i.Id == itemId)
                    ?? throw new NotFoundException(nameof(BillItem), itemId);
                BillMath.ApplyItemDiscountPercent(item, approval.RequestedPercent);
            }
            else
            {
                bill.BillDiscountPercent = approval.RequestedPercent;
            }
            BillMath.RecomputeBillTotals(bill);
            approval.Status = DiscountApprovalStatus.Approved;
            await activity.LogAsync(ActivityEvents.DiscountApproved,
                $"Discount of {approval.RequestedPercent}% approved on bill {bill.BillNumber}", ct: ct);
        }
        else
        {
            approval.Status = DiscountApprovalStatus.Rejected;
            approval.RejectionReason = request.RejectionReason;
            await activity.LogAsync(ActivityEvents.DiscountRejected,
                $"Discount of {approval.RequestedPercent}% rejected on bill {bill.BillNumber}: {request.RejectionReason}", ct: ct);
        }
        approval.DecidedByUserId = currentUser.UserId;
        approval.DecidedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    public async Task<BillPaymentDto> CollectPaymentAsync(long billId, CollectPaymentRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            // Serialise concurrent payments on the same bill (double-click / two counters): the second
            // request waits here and then re-reads the balance the first one committed.
            await unitOfWork.LockAsync<Bill>(billId, token);
            var bill = await db.Bills.FirstOrDefaultAsync(b => b.Id == billId, token)
                ?? throw new NotFoundException(nameof(Bill), billId);
            await EnsureCanAccessBillTypeAsync(bill.BillType, token);
            if (bill.Status is BillStatus.Paid or BillStatus.Cancelled or BillStatus.Parked)
            {
                throw new DomainException($"A bill that is {bill.Status} cannot accept payment.");
            }
            if (request.Amount > bill.BalanceAmount)
            {
                throw new DomainException("Payment amount exceeds the outstanding balance.");
            }

            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            await DayCloseGuard.EnsureOpenAsync(db, bill.BranchId, today, token);

            var mode = await db.PaymentModes.FirstOrDefaultAsync(m => m.Id == request.PaymentModeId, token)
                ?? throw new NotFoundException(nameof(PaymentMode), request.PaymentModeId);
            if (mode.RequiresReference && string.IsNullOrWhiteSpace(request.ReferenceNumber))
            {
                throw new DomainException($"{mode.Name} payments require a reference number.");
            }
            if (mode.RequiresReference)
            {
                // A UPI/card/bank transaction reference identifies one real transfer; reusing it on
                // another payment is either a keying error or a duplicate claim of the same money.
                var reference = request.ReferenceNumber!.Trim().ToUpper();
                if (await db.BillPayments.AnyAsync(x => x.PaymentModeId == mode.Id && x.ReferenceNumber != null &&
                        x.ReferenceNumber.ToUpper() == reference, token))
                {
                    throw new DomainException($"{mode.Name} reference {request.ReferenceNumber!.Trim()} has already been used on another payment.");
                }
            }

            var year = today.Year;
            var receiptValue = await sequences.NextValueAsync(SequenceKeys.ReceiptNumber, bill.BranchId, year, token);
            var openShiftId = await db.CashierShifts.AsNoTracking()
                .Where(s => s.UserId == currentUser.UserId && s.Status == CashierShiftStatus.Open)
                .Select(s => (long?)s.Id).FirstOrDefaultAsync(token);
            var payment = new BillPayment
            {
                BillId = bill.Id,
                PaymentModeId = mode.Id,
                PaymentMode = mode,
                Amount = request.Amount,
                ReferenceNumber = request.ReferenceNumber,
                ReceiptNumber = $"RCPT-{year}-{receiptValue.ToString().PadLeft(6, '0')}",
                PaidAt = timeProvider.GetUtcNow(),
                CashierShiftId = openShiftId,
            };
            db.BillPayments.Add(payment);

            bill.PaidAmount += request.Amount;
            bill.BalanceAmount = Math.Max(bill.TotalAmount - bill.PaidAmount, 0);
            bill.Status = bill.BalanceAmount <= 0 ? BillStatus.Paid : BillStatus.PartiallyPaid;

            await db.SaveChangesAsync(token);
            // Finance's auto-posting hook (plan §18) — a deliberate no-op until the branch
            // links this payment mode to a Finance account, so Billing works standalone.
            await ledgerPoster.PostBillPaymentAsync(bill.Id, bill.BillNumber, mode.Id, request.Amount, today, token);
            return new BillPaymentDto(payment.Id, mode.Id, mode.Name, payment.Amount, payment.ReferenceNumber,
                payment.ReceiptNumber, payment.PaidAt);
        }, ct);

    /// <summary>
    /// Draws down a patient's deposit against this bill's balance — the previously-documented
    /// gap in AdvanceService: "deposits stay separately visible/collectible, not yet consumed
    /// by billing." Reuses the exact PaidAmount/BalanceAmount/Status recompute CollectPaymentAsync
    /// already does; no cash actually moves, so no BillPayment/receipt is written.
    /// </summary>
    public async Task DrawAdvanceAsync(long billId, DrawAdvanceOnBillRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await unitOfWork.LockAsync<Bill>(billId, token);
            await unitOfWork.LockAsync<Advance>(request.AdvanceId, token);
            var bill = await db.Bills.FirstOrDefaultAsync(b => b.Id == billId, token)
                ?? throw new NotFoundException(nameof(Bill), billId);
            await EnsureCanAccessBillTypeAsync(bill.BillType, token);
            if (bill.Status is BillStatus.Paid or BillStatus.Cancelled or BillStatus.Parked)
            {
                throw new DomainException($"A bill that is {bill.Status} cannot draw down an advance.");
            }
            if (request.Amount > bill.BalanceAmount)
            {
                throw new DomainException("Draw amount exceeds the outstanding balance.");
            }

            var advance = await db.Advances.FirstOrDefaultAsync(a => a.Id == request.AdvanceId, token)
                ?? throw new NotFoundException(nameof(Advance), request.AdvanceId);
            if (advance.PatientId != bill.PatientId)
            {
                throw new DomainException("This advance was not collected against the bill's patient.");
            }
            if (request.Amount > advance.BalanceAmount)
            {
                throw new DomainException("Draw amount exceeds the advance's remaining balance.");
            }

            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            await DayCloseGuard.EnsureOpenAsync(db, bill.BranchId, today, token);

            advance.BalanceAmount -= request.Amount;
            bill.PaidAmount += request.Amount;
            bill.BalanceAmount = Math.Max(bill.TotalAmount - bill.PaidAmount, 0);
            bill.Status = bill.BalanceAmount <= 0 ? BillStatus.Paid : BillStatus.PartiallyPaid;

            await activity.LogAsync(ActivityEvents.AdvanceDrawn,
                $"Advance drawn down by {request.Amount:0.00} against bill {bill.BillNumber}", ct: token);
            await db.SaveChangesAsync(token);
        }, ct);

    // ── Bill notes ───────────────────────────────────────────────────────────
    public async Task AddNoteAsync(long billId, AddBillNoteRequest request, CancellationToken ct = default)
    {
        var billForAccess = await db.Bills.AsNoTracking().FirstOrDefaultAsync(b => b.Id == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);
        await EnsureCanAccessBillTypeAsync(billForAccess.BillType, ct);
        db.BillNotes.Add(new BillNote { BillId = billId, Note = request.Note });
        var bill = await db.Bills.AsNoTracking().Where(b => b.Id == billId).Select(b => b.BillNumber).FirstAsync(ct);
        await activity.LogAsync(ActivityEvents.BillNoteAdded, $"Note added on bill {bill}", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<BillNoteDto>> GetNotesAsync(long billId, CancellationToken ct = default) =>
        await GetNotesAfterAccessCheckAsync(billId, ct);

    private async Task<IReadOnlyList<BillNoteDto>> GetNotesAfterAccessCheckAsync(long billId, CancellationToken ct)
    {
        var bill = await db.Bills.AsNoTracking().FirstOrDefaultAsync(b => b.Id == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);
        await EnsureCanAccessBillTypeAsync(bill.BillType, ct);
        return await db.BillNotes.AsNoTracking()
            .Where(n => n.BillId == billId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new BillNoteDto(n.Id, n.Note, n.CreatedBy, null, n.CreatedAt))
            .ToListAsync(ct);
    }

    // ── Bill parking ─────────────────────────────────────────────────────────
    /// <summary>Puts an open/partially-paid bill on hold — blocks further edits/payments/discounts, same as Cancelled/Paid.</summary>
    public async Task ParkAsync(long billId, ParkBillRequest request, CancellationToken ct = default)
    {
        var bill = await db.Bills.FirstOrDefaultAsync(b => b.Id == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);
        await EnsureCanAccessBillTypeAsync(bill.BillType, ct);
        if (bill.Status is not (BillStatus.Open or BillStatus.PartiallyPaid))
        {
            throw new DomainException($"A bill that is {bill.Status} cannot be parked.");
        }
        bill.Status = BillStatus.Parked;
        await activity.LogAsync(ActivityEvents.BillParked, $"Bill {bill.BillNumber} parked: {request.Reason}", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Restores a parked bill to the status its amounts imply — same recompute DecideRefundAsync uses.</summary>
    public async Task UnparkAsync(long billId, CancellationToken ct = default)
    {
        var bill = await db.Bills.FirstOrDefaultAsync(b => b.Id == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);
        await EnsureCanAccessBillTypeAsync(bill.BillType, ct);
        if (bill.Status != BillStatus.Parked)
        {
            throw new DomainException("This bill is not parked.");
        }
        bill.Status = bill.PaidAmount <= 0
            ? BillStatus.Open
            : (bill.BalanceAmount <= 0 ? BillStatus.Paid : BillStatus.PartiallyPaid);
        await activity.LogAsync(ActivityEvents.BillUnparked, $"Bill {bill.BillNumber} unparked", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(long billId, CancelBillRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token => await CancelLockedAsync(billId, request, token), ct);

    private async Task CancelLockedAsync(long billId, CancelBillRequest request, CancellationToken ct)
    {
        await unitOfWork.LockAsync<Bill>(billId, ct);
        var bill = await db.Bills.FirstOrDefaultAsync(b => b.Id == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);
        await EnsureCanAccessBillTypeAsync(bill.BillType, ct);
        if (bill.Status == BillStatus.Cancelled)
        {
            throw new DomainException("This bill is already cancelled.");
        }
        if (bill.PaidAmount > 0)
        {
            throw new DomainException("Refund all payments before cancelling the bill.");
        }

        bill.Status = BillStatus.Cancelled;
        bill.CancelReason = request.Reason;
        bill.CancelledAt = timeProvider.GetUtcNow();

        await activity.LogAsync(ActivityEvents.BillCancelled, $"Bill {bill.BillNumber} cancelled: {request.Reason}", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task MakeCreditBillAsync(long billId, MakeCreditBillRequest request, CancellationToken ct = default)
    {
        var bill = await db.Bills.FirstOrDefaultAsync(b => b.Id == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);
        await EnsureCanAccessBillTypeAsync(bill.BillType, ct);
        if (bill.Status != BillStatus.Open)
        {
            throw new DomainException("Only an open, unpaid bill can be marked as a credit bill.");
        }
        if (request.CorporateId is { } corporateId)
        {
            var corporate = await db.Corporates.FirstOrDefaultAsync(c => c.Id == corporateId, ct)
                ?? throw new NotFoundException(nameof(Corporate), corporateId);
            if (corporate.CreditLimit is > 0)
            {
                var exposure = await db.Bills.Where(b => b.CorporateId == corporateId && b.IsCreditBill && b.Id != bill.Id &&
                        b.Status != BillStatus.Cancelled && b.BalanceAmount > 0)
                    .SumAsync(b => (decimal?)b.BalanceAmount, ct) ?? 0;
                if (exposure + bill.BalanceAmount > corporate.CreditLimit)
                {
                    throw new DomainException(
                        $"Credit limit of {corporate.CreditLimit:0.00} for {corporate.Name} would be exceeded " +
                        $"(outstanding {exposure:0.00} + this bill {bill.BalanceAmount:0.00}).");
                }
            }
        }

        bill.IsCreditBill = true;
        bill.CorporateId = request.CorporateId;
        await db.SaveChangesAsync(ct);
    }

    public async Task<RefundDto> RequestRefundAsync(long billId, RequestRefundRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token => await RequestRefundLockedAsync(billId, request, token), ct);

    private async Task<RefundDto> RequestRefundLockedAsync(long billId, RequestRefundRequest request, CancellationToken ct)
    {
        await unitOfWork.LockAsync<Bill>(billId, ct);
        var bill = await db.Bills.FirstOrDefaultAsync(b => b.Id == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);
        await EnsureCanAccessBillTypeAsync(bill.BillType, ct);
        if (bill.PaidAmount <= 0)
        {
            throw new DomainException("This bill has no payments to refund.");
        }

        // Pending/Approved refunds will come out of PaidAmount when issued, so they already count
        // against what is refundable now (Issued refunds have already reduced PaidAmount).
        var uncommitted = await db.Refunds.Where(r => r.BillId == billId &&
                (r.Status == RefundStatus.Pending || r.Status == RefundStatus.Approved))
            .SumAsync(r => (decimal?)r.Amount, ct) ?? 0;
        var refundable = Math.Max(bill.PaidAmount - uncommitted, 0);
        if (request.Amount > refundable)
        {
            throw new DomainException($"Refund amount exceeds what is still refundable on this bill ({refundable:0.00}).");
        }

        BillPayment? payment = null;
        if (request.BillPaymentId is { } paymentId)
        {
            payment = await db.BillPayments.FirstOrDefaultAsync(x => x.Id == paymentId && x.BillId == billId, ct)
                ?? throw new NotFoundException(nameof(BillPayment), paymentId);
            var againstPayment = await db.Refunds.Where(r => r.BillPaymentId == paymentId && r.Status != RefundStatus.Rejected)
                .SumAsync(r => (decimal?)r.Amount, ct) ?? 0;
            if (request.Amount > payment.Amount - againstPayment)
            {
                throw new DomainException(
                    $"Refund amount exceeds what is still refundable on the selected payment ({Math.Max(payment.Amount - againstPayment, 0):0.00}).");
            }
        }

        var refund = new Refund
        {
            BillId = bill.Id,
            BillPaymentId = request.BillPaymentId,
            Amount = request.Amount,
            Reason = request.Reason,
            Method = request.Method,
        };
        db.Refunds.Add(refund);
        await activity.LogAsync(ActivityEvents.RefundRequested,
            $"Refund of {request.Amount:0.00} requested on bill {bill.BillNumber}: {request.Reason}", ct: ct);
        await db.SaveChangesAsync(ct);
        return ToRefundDto(refund, bill.BillNumber);
    }

    /// <summary>Requests a refund against an advance's unused deposit — no bill-type access check, since an advance isn't tied to a bill type.</summary>
    public async Task<RefundDto> RequestAdvanceRefundAsync(long advanceId, RequestAdvanceRefundRequest request, CancellationToken ct = default)
    {
        var advance = await db.Advances.FirstOrDefaultAsync(a => a.Id == advanceId, ct)
            ?? throw new NotFoundException(nameof(Advance), advanceId);
        if (request.Amount <= 0 || request.Amount > advance.BalanceAmount)
        {
            throw new DomainException("Refund amount must be positive and cannot exceed the advance's remaining balance.");
        }

        var refund = new Refund
        {
            AdvanceId = advance.Id,
            Amount = request.Amount,
            Reason = request.Reason,
            Method = request.Method,
        };
        db.Refunds.Add(refund);
        await activity.LogAsync(ActivityEvents.RefundRequested,
            $"Refund of {request.Amount:0.00} requested on advance #{advance.Id}: {request.Reason}", ct: ct);
        await db.SaveChangesAsync(ct);
        return ToRefundDto(refund, null);
    }

    /// <summary>Refunds awaiting an Approve/Reject decision — both flavors (bill-scoped, narrowed to allowed bill types; advance-scoped, unscoped) in one queue.</summary>
    public async Task<IReadOnlyList<RefundDto>> GetPendingRefundsAsync(CancellationToken ct = default)
    {
        var allowedTypes = await GetAllowedBillTypesAsync(ct);
        return await db.Refunds.AsNoTracking()
            .Where(r => r.Status == RefundStatus.Pending && (r.BillId == null || allowedTypes.Contains(r.Bill!.BillType)))
            .Include(r => r.Bill)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new RefundDto(
                r.Id, r.BillId, r.Bill != null ? r.Bill.BillNumber : null, r.BillPaymentId, r.AdvanceId, r.Amount, r.Reason, r.Method, r.Status,
                r.ApprovedByUserId, null, r.ApprovedAt, r.IssuedByUserId, null, r.IssuedAt, r.RejectionReason, r.CreatedAt))
            .ToListAsync(ct);
    }

    /// <summary>Approved refunds not yet paid out — the Issue queue.</summary>
    public async Task<IReadOnlyList<RefundDto>> GetIssuableRefundsAsync(CancellationToken ct = default)
    {
        var allowedTypes = await GetAllowedBillTypesAsync(ct);
        return await db.Refunds.AsNoTracking()
            .Where(r => r.Status == RefundStatus.Approved && (r.BillId == null || allowedTypes.Contains(r.Bill!.BillType)))
            .Include(r => r.Bill)
            .OrderBy(r => r.ApprovedAt)
            .Select(r => new RefundDto(
                r.Id, r.BillId, r.Bill != null ? r.Bill.BillNumber : null, r.BillPaymentId, r.AdvanceId, r.Amount, r.Reason, r.Method, r.Status,
                r.ApprovedByUserId, null, r.ApprovedAt, r.IssuedByUserId, null, r.IssuedAt, r.RejectionReason, r.CreatedAt))
            .ToListAsync(ct);
    }

    /// <summary>Approve/reject a refund request — a decision only, no money moves yet (see IssueRefundAsync).</summary>
    public async Task DecideRefundAsync(long refundId, DecideRefundRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var refund = await db.Refunds.Include(r => r.Bill).FirstOrDefaultAsync(r => r.Id == refundId, token)
                ?? throw new NotFoundException(nameof(Refund), refundId);
            if (refund.Status != RefundStatus.Pending)
            {
                throw new DomainException($"A refund that is {refund.Status} cannot be decided again.");
            }
            if (refund.Bill is { } bill)
            {
                await EnsureCanAccessBillTypeAsync(bill.BillType, token);
            }

            var subject = refund.Bill is { } b ? $"bill {b.BillNumber}" : $"advance #{refund.AdvanceId}";
            if (request.Approve)
            {
                refund.Status = RefundStatus.Approved;
                refund.ApprovedByUserId = currentUser.UserId;
                refund.ApprovedAt = timeProvider.GetUtcNow();
                await activity.LogAsync(ActivityEvents.RefundApproved, $"Refund of {refund.Amount:0.00} approved on {subject}", ct: token);
            }
            else
            {
                refund.Status = RefundStatus.Rejected;
                refund.RejectionReason = request.RejectionReason;
                await activity.LogAsync(ActivityEvents.RefundRejected,
                    $"Refund of {refund.Amount:0.00} rejected on {subject}: {request.RejectionReason}", ct: token);
            }
            await db.SaveChangesAsync(token);
        }, ct);

    /// <summary>Pays out an already-approved refund — the money actually moves here: a bill refund reverses PaidAmount/BalanceAmount/Status, an advance refund reduces BalanceAmount. Separate step from Approve so a supervisor's decision and a cashier's cash handout aren't the same click.</summary>
    public async Task IssueRefundAsync(long refundId, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            // Lock the refund and the money it moves before reading them, so two "Issue" clicks (or an
            // issue racing another refund on the same bill) cannot both pass the checks below.
            var target = await db.Refunds.AsNoTracking().Where(r => r.Id == refundId)
                .Select(r => new { r.BillId, r.AdvanceId }).FirstOrDefaultAsync(token)
                ?? throw new NotFoundException(nameof(Refund), refundId);
            await unitOfWork.LockAsync<Refund>(refundId, token);
            if (target.BillId is { } lockBillId) await unitOfWork.LockAsync<Bill>(lockBillId, token);
            if (target.AdvanceId is { } lockAdvanceId) await unitOfWork.LockAsync<Advance>(lockAdvanceId, token);

            var refund = await db.Refunds.Include(r => r.Bill).Include(r => r.Advance).FirstOrDefaultAsync(r => r.Id == refundId, token)
                ?? throw new NotFoundException(nameof(Refund), refundId);
            if (refund.Status != RefundStatus.Approved)
            {
                throw new DomainException($"A refund that is {refund.Status} cannot be issued — it must be Approved first.");
            }

            string subject;
            if (refund.Bill is { } bill)
            {
                await EnsureCanAccessBillTypeAsync(bill.BillType, token);
                if (refund.Amount > bill.PaidAmount)
                {
                    throw new DomainException(
                        $"Cannot issue a refund of {refund.Amount:0.00}: only {bill.PaidAmount:0.00} remains paid on bill {bill.BillNumber}.");
                }
                bill.PaidAmount -= refund.Amount;
                bill.BalanceAmount = Math.Max(bill.TotalAmount - bill.PaidAmount, 0);
                bill.Status = bill.PaidAmount <= 0
                    ? BillStatus.Open
                    : (bill.BalanceAmount <= 0 ? BillStatus.Paid : BillStatus.PartiallyPaid);
                subject = $"bill {bill.BillNumber}";
            }
            else
            {
                var advance = refund.Advance!;
                if (refund.Amount > advance.BalanceAmount)
                {
                    throw new DomainException($"Cannot issue a refund of {refund.Amount:0.00}: the advance only has {advance.BalanceAmount:0.00} left.");
                }
                advance.BalanceAmount -= refund.Amount;
                subject = $"advance #{advance.Id}";
            }

            refund.Status = RefundStatus.Issued;
            refund.IssuedByUserId = currentUser.UserId;
            refund.IssuedAt = timeProvider.GetUtcNow();
            await activity.LogAsync(ActivityEvents.RefundIssued, $"Refund of {refund.Amount:0.00} issued on {subject}", ct: token);
            await db.SaveChangesAsync(token);
        }, ct);

    // ── Bill credit notes ────────────────────────────────────────────────────
    /// <summary>Requests a post-finalization value adjustment against specific lines of a Paid/PartiallyPaid bill.</summary>
    public async Task<CreditNoteDto> RequestCreditNoteAsync(long billId, RequestCreditNoteRequest request, CancellationToken ct = default)
    {
        var bill = await db.Bills.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);
        await EnsureCanAccessBillTypeAsync(bill.BillType, ct);
        if (bill.Status is not (BillStatus.Paid or BillStatus.PartiallyPaid))
        {
            throw new DomainException("A credit note can only be raised against a paid or partially paid bill.");
        }

        var creditNote = new BillCreditNote
        {
            BillId = bill.Id,
            CreditNoteNumber = string.Empty,
            CreditNoteDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
            Reason = request.Reason,
        };
        foreach (var line in request.Items)
        {
            var item = bill.Items.FirstOrDefault(i => i.Id == line.BillItemId)
                ?? throw new NotFoundException(nameof(BillItem), line.BillItemId);
            if (line.Amount > item.TotalAmount)
            {
                throw new DomainException($"Credit note amount for '{item.Description}' exceeds the line's billed amount.");
            }
            creditNote.Items.Add(new BillCreditNoteItem { BillItemId = item.Id, Amount = line.Amount });
        }
        creditNote.Amount = creditNote.Items.Sum(i => i.Amount);
        if (creditNote.Amount <= 0)
        {
            throw new DomainException("A credit note needs at least one line with a positive amount.");
        }

        var year = creditNote.CreditNoteDate.Year;
        var number = await sequences.NextValueAsync(SequenceKeys.CreditNoteNumber, bill.BranchId, year, ct);
        creditNote.CreditNoteNumber = $"CN-{year}-{number.ToString().PadLeft(6, '0')}";

        db.BillCreditNotes.Add(creditNote);
        await activity.LogAsync(ActivityEvents.CreditNoteRequested,
            $"Credit note of {creditNote.Amount:0.00} requested on bill {bill.BillNumber}: {request.Reason}", ct: ct);
        await db.SaveChangesAsync(ct);
        return await GetCreditNoteByIdAsync(creditNote.Id, ct);
    }

    public async Task<IReadOnlyList<CreditNoteDto>> GetPendingCreditNotesAsync(CancellationToken ct = default)
    {
        var allowedTypes = await GetAllowedBillTypesAsync(ct);
        return await db.BillCreditNotes.AsNoTracking()
            .Where(c => c.Status == CreditNoteStatus.Pending && allowedTypes.Contains(c.Bill!.BillType))
            .Include(c => c.Bill)
            .Include(c => c.Items).ThenInclude(i => i.BillItem)
            .OrderBy(c => c.CreatedAt)
            .Select(c => ToCreditNoteDto(c))
            .ToListAsync(ct);
    }

    public async Task DecideCreditNoteAsync(long creditNoteId, DecideCreditNoteRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var creditNote = await db.BillCreditNotes.Include(c => c.Bill).ThenInclude(b => b!.Items)
                .FirstOrDefaultAsync(c => c.Id == creditNoteId, token)
                ?? throw new NotFoundException(nameof(BillCreditNote), creditNoteId);
            if (creditNote.Status != CreditNoteStatus.Pending)
            {
                throw new DomainException($"A credit note that is {creditNote.Status} cannot be decided again.");
            }

            var bill = creditNote.Bill!;
            await EnsureCanAccessBillTypeAsync(bill.BillType, token);
            if (request.Approve)
            {
                bill.TotalAmount -= creditNote.Amount;
                bill.BalanceAmount = Math.Max(bill.TotalAmount - bill.PaidAmount, 0);
                creditNote.Status = CreditNoteStatus.Approved;
                creditNote.DecidedByUserId = currentUser.UserId;
                creditNote.DecidedAt = timeProvider.GetUtcNow();
                await activity.LogAsync(ActivityEvents.CreditNoteApproved,
                    $"Credit note {creditNote.CreditNoteNumber} of {creditNote.Amount:0.00} approved on bill {bill.BillNumber}", ct: token);
                await db.SaveChangesAsync(token);
                await ledgerPoster.PostCreditNoteAsync(bill.Id, bill.BillNumber, creditNote.CreditNoteNumber, creditNote.Amount, creditNote.CreditNoteDate, token);
            }
            else
            {
                creditNote.Status = CreditNoteStatus.Rejected;
                creditNote.RejectionReason = request.RejectionReason;
                creditNote.DecidedByUserId = currentUser.UserId;
                creditNote.DecidedAt = timeProvider.GetUtcNow();
                await activity.LogAsync(ActivityEvents.CreditNoteRejected,
                    $"Credit note {creditNote.CreditNoteNumber} rejected on bill {bill.BillNumber}: {request.RejectionReason}", ct: token);
                await db.SaveChangesAsync(token);
            }
        }, ct);

    private async Task<CreditNoteDto> GetCreditNoteByIdAsync(long id, CancellationToken ct)
    {
        var c = await db.BillCreditNotes.AsNoTracking()
            .Include(c => c.Bill)
            .Include(c => c.Items).ThenInclude(i => i.BillItem)
            .FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException(nameof(BillCreditNote), id);
        await EnsureCanAccessBillTypeAsync(c.Bill!.BillType, ct);
        return ToCreditNoteDto(c);
    }

    private static CreditNoteDto ToCreditNoteDto(BillCreditNote c) => new(
        c.Id, c.BillId, c.Bill!.BillNumber, c.CreditNoteNumber, c.CreditNoteDate, c.Reason, c.Amount, c.Status,
        c.DecidedByUserId, null, c.DecidedAt, c.RejectionReason, c.CreatedAt,
        c.Items.Select(i => new CreditNoteItemDto(i.Id, i.BillItemId, i.BillItem!.Description, i.Amount)).ToList());

    public async Task<byte[]> GenerateReceiptAsync(long billId, long? paymentId, CancellationToken ct = default)
    {
        var bill = await db.Bills.AsNoTracking()
            .Include(b => b.Patient)
            .Include(b => b.Items)
            .Include(b => b.Payments).ThenInclude(p => p.PaymentMode)
            .FirstOrDefaultAsync(b => b.Id == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);
        await EnsureCanAccessBillTypeAsync(bill.BillType, ct);
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bill.BranchId, ct);

        var payment = paymentId is { } id
            ? bill.Payments.FirstOrDefault(p => p.Id == id) ?? throw new NotFoundException(nameof(BillPayment), id)
            : bill.Payments.OrderByDescending(p => p.PaidAt).FirstOrDefault();

        var footerNote = await db.SystemSettings.AsNoTracking()
            .Where(s => s.Key == SettingKeys.BillingReceiptFooterNote).Select(s => s.Value).FirstOrDefaultAsync(ct);

        var data = new BillReceiptData(
            bill.BillNumber, bill.BillDate, FormatName(bill.Patient!), bill.Patient!.Uhid,
            branch?.Name ?? string.Empty, branch?.Address ?? string.Empty,
            bill.Items.Select(i => new BillReceiptLine(i.Description, i.Quantity, i.Rate, i.DiscountAmount, i.TaxAmount, i.TotalAmount)).ToList(),
            bill.SubTotal, bill.DiscountAmount, bill.TaxAmount, bill.TotalAmount, bill.PaidAmount, bill.BalanceAmount,
            payment?.ReceiptNumber, payment?.PaymentMode?.Name, payment?.Amount, footerNote);
        return pdf.GenerateBillReceipt(data);
    }

    // ── Discount validation helpers ─────────────────────────────────────────

    /// <summary>
    /// No DiscountTypeId means an ad-hoc discount, which always routes to approval
    /// (threshold treated as 0). A DiscountTypeId caps the percent at MaxPercent and
    /// uses its own ApprovalThresholdPercent.
    /// </summary>
    private async Task<(decimal Percent, DiscountType? Type)> ValidateDiscountAsync(
        long? discountTypeId, decimal percent, CancellationToken ct)
    {
        if (discountTypeId is not { } id)
        {
            return (percent, null);
        }
        var type = await db.DiscountTypes.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(DiscountType), id);
        if (percent > type.MaxPercent)
        {
            throw new DomainException($"{type.Name} allows a maximum discount of {type.MaxPercent}%.");
        }
        return (percent, type);
    }

    private Task<decimal> ResolveServiceRateAsync(
        long branchId, Service service, long? patientCategoryId, DateOnly asOf, long? roomTypeId, CancellationToken ct) =>
        IpBillAccess.ResolveServiceRateAsync(db, branchId, service, patientCategoryId, asOf, roomTypeId, ct);

    /// <summary>
    /// "Pending order retrieval": puts every not-yet-billed lab test / radiology scan of this bill's
    /// visit, admission or ER visit onto the bill at the tariff price of the test's billing service,
    /// and back-links each order item to its bill line (which is what the diagnostics billing gate
    /// checks). A test whose billing service is not mapped is added at 0.00 and labelled, so it is
    /// visible to the cashier rather than silently free.
    /// </summary>
    private async Task<int> PostPendingDiagnosticsAsync(Bill bill, CancellationToken ct)
    {
        var labQuery = db.LabOrderItems.Include(i => i.LabTest).Include(i => i.LabOrder)
            .Where(i => i.BillItemId == null && i.Status != LabOrderItemStatus.Cancelled);
        var radQuery = db.RadOrderItems.Include(i => i.RadiologyTest).Include(i => i.RadOrder)
            .Where(i => i.BillItemId == null && i.Status != RadOrderItemStatus.Cancelled);
        if (bill.OpVisitId is { } visitId)
        {
            labQuery = labQuery.Where(i => i.LabOrder!.OpVisitId == visitId && i.LabOrder.AdmissionId == null);
            radQuery = radQuery.Where(i => i.RadOrder!.OpVisitId == visitId && i.RadOrder.AdmissionId == null);
        }
        else if (bill.AdmissionId is { } admissionId)
        {
            labQuery = labQuery.Where(i => i.LabOrder!.AdmissionId == admissionId);
            radQuery = radQuery.Where(i => i.RadOrder!.AdmissionId == admissionId);
        }
        else if (bill.EmergencyVisitId is { } erId)
        {
            labQuery = labQuery.Where(i => i.LabOrder!.EmergencyVisitId == erId);
            radQuery = radQuery.Where(i => i.RadOrder!.EmergencyVisitId == erId);
        }
        else
        {
            return 0;
        }

        var labs = await labQuery.ToListAsync(ct);
        var rads = await radQuery.ToListAsync(ct);
        if (labs.Count == 0 && rads.Count == 0)
        {
            return 0;
        }

        var serviceIds = labs.Select(l => l.LabTest!.ServiceId).Concat(rads.Select(r => r.RadiologyTest!.ServiceId))
            .Where(id => id != null).Select(id => id!.Value).Distinct().ToList();
        var services = await db.Services.Include(s => s.Tax).Where(s => serviceIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct);
        var patientCategoryId = await db.Patients.AsNoTracking().Where(p => p.Id == bill.PatientId).Select(p => p.PatientCategoryId).FirstOrDefaultAsync(ct);
        var roomTypeId = bill.AdmissionId is { } admForRate
            ? await db.Admissions.AsNoTracking().Where(a => a.Id == admForRate).Select(a => (long?)a.CurrentBed!.Room!.RoomTypeId).FirstOrDefaultAsync(ct)
            : null;

        async Task<BillItem> LineAsync(BillItemSourceType type, string prefix, string name, long? serviceId)
        {
            if (serviceId is { } sid && services.TryGetValue(sid, out var service))
            {
                var rate = await ResolveServiceRateAsync(bill.BranchId, service, patientCategoryId, bill.BillDate, roomTypeId, ct);
                var tax = service.IsTaxable ? (service.Tax?.RatePercent ?? 0) : 0;
                return IpBillAccess.AddLine(bill, type, $"{prefix} — {name}", 1, rate, tax, service.Id);
            }
            return IpBillAccess.AddLine(bill, type, $"{prefix} — {name} (price not configured)", 1, 0);
        }

        var links = new List<(BillItem Line, Action<long> Link)>();
        foreach (var lab in labs)
        {
            var line = await LineAsync(BillItemSourceType.LabTest, "Lab", lab.LabTest!.Name, lab.LabTest.ServiceId);
            links.Add((line, id => lab.BillItemId = id));
        }
        foreach (var rad in rads)
        {
            var line = await LineAsync(BillItemSourceType.RadiologyTest, "Radiology", rad.RadiologyTest!.Name, rad.RadiologyTest.ServiceId);
            links.Add((line, id => rad.BillItemId = id));
        }
        await db.SaveChangesAsync(ct);
        foreach (var (line, link) in links)
        {
            link(line.Id);
        }
        await db.SaveChangesAsync(ct);
        return links.Count;
    }

    /// <summary>Adds lab/radiology orders raised after the bill was generated (works on a settled bill too — it becomes partially paid).</summary>
    public async Task<BillDetailDto> AddPendingOrdersAsync(long billId, CancellationToken ct = default)
    {
        var bill = await db.Bills.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);
        await EnsureCanAccessBillTypeAsync(bill.BillType, ct);
        if (bill.Status is BillStatus.Cancelled or BillStatus.Parked)
        {
            throw new DomainException($"A bill that is {bill.Status} cannot take new charges.");
        }
        var added = await PostPendingDiagnosticsAsync(bill, ct);
        if (added == 0)
        {
            throw new DomainException("There are no unbilled lab or radiology orders for this patient episode.");
        }
        return await GetByIdAsync(bill.Id, ct);
    }

    private async Task<Bill> LoadEditableBillAsync(long billId, CancellationToken ct)
    {
        var bill = await db.Bills.Include(b => b.Items).Include(b => b.Patient)
            .FirstOrDefaultAsync(b => b.Id == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);
        await EnsureCanAccessBillTypeAsync(bill.BillType, ct);
        if (bill.Status is BillStatus.Paid or BillStatus.Cancelled or BillStatus.Parked)
        {
            throw new DomainException($"A bill that is {bill.Status} can no longer be edited.");
        }
        return bill;
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch before billing.");

    private static string FormatName(Patient patient) =>
        string.Join(' ', new[] { patient.FirstName, patient.MiddleName, patient.LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

    private static DiscountApprovalDto ToDiscountApprovalDto(DiscountApproval a, string billNumber, string? itemDescription) =>
        new(a.Id, a.BillId, billNumber, a.BillItemId, itemDescription, a.RequestedPercent, a.RequestedAmount,
            a.Reason, a.Status, a.DecidedByUserId, null, a.DecidedAt, a.RejectionReason, a.CreatedAt);

    private static RefundDto ToRefundDto(
        Refund r, string? billNumber, string? approvedByName = null, string? issuedByName = null) =>
        new(r.Id, r.BillId, billNumber, r.BillPaymentId, r.AdvanceId, r.Amount, r.Reason, r.Method, r.Status,
            r.ApprovedByUserId, approvedByName, r.ApprovedAt, r.IssuedByUserId, issuedByName, r.IssuedAt, r.RejectionReason, r.CreatedAt);

    private static BillDetailDto ToDetailDto(Bill b, decimal? creditLimit = null, decimal? creditExposure = null, BillInsuranceStatusDto? insuranceStatus = null) => new(
        b.Id, b.BillNumber, b.BillDate, b.PatientId, b.Patient!.Uhid, FormatName(b.Patient!), b.Patient!.Mobile,
        b.OpVisitId, b.AdmissionId, b.EmergencyVisitId, b.BillType, b.Status, b.SubTotal, b.DiscountAmount, b.TaxAmount, b.TotalAmount, b.PaidAmount,
        b.BalanceAmount, b.IsCreditBill, b.CorporateId, b.Corporate?.Name, b.CancelReason, b.CancelledAt, creditLimit, creditExposure, insuranceStatus,
        b.Items.OrderBy(i => i.Id).Select(i => new BillItemDto(
            i.Id, i.SourceType, i.ServiceId, i.Service?.Name, i.PackageId, i.Package?.Name, i.ClinicalOrderId,
            i.Description, i.Quantity, i.Rate, i.DiscountPercent, i.DiscountAmount, i.TaxPercent, i.TaxAmount, i.TotalAmount)).ToList(),
        b.Payments.OrderBy(p => p.PaidAt).Select(p => new BillPaymentDto(
            p.Id, p.PaymentModeId, p.PaymentMode!.Name, p.Amount, p.ReferenceNumber, p.ReceiptNumber, p.PaidAt)).ToList());
}
