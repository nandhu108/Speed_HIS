using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Auth;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Ipd;

/// <summary>
/// Discharge request → 4-way clearance checklist → discharge summary (doctor-approved) →
/// final discharge → discharge slip (plan §9). Requesting/clearing/summarizing write
/// nothing to beds or billing, so they're plain saves; FinalizeDischargeAsync is the one
/// CLAUDE.md-mandated transaction ("discharge") since it releases the bed. The Billing
/// clearance is still cleared manually by billing staff after settling the IP bill through the
/// existing Billing screens — GetClearancesAsync surfaces the live Bill.BalanceAmount alongside
/// it so that's an informed self-certification rather than a blind one, but clearing is not
/// blocked on it (docs/PATIENT_JOURNEY_GAP_ANALYSIS.md 5.2 -- advisory only, by design).
/// </summary>
public class DischargeService(
    IAppDbContext db, IUnitOfWork unitOfWork, ICurrentUser currentUser, IPdfService pdf, TimeProvider timeProvider, PermissionService permissions)
{
    /// <summary>Unsettled IP bills of an admission (balance due and not a corporate/insurance credit bill).</summary>
    private async Task<string?> UnsettledBillAsync(long admissionId, CancellationToken ct) =>
        await db.Bills.AsNoTracking()
            .Where(b => b.AdmissionId == admissionId && b.Status != BillStatus.Cancelled && b.BalanceAmount > 0 && !b.IsCreditBill)
            .Select(b => b.BillNumber + " (" + b.BalanceAmount + " due)").FirstOrDefaultAsync(ct);

    public async Task RequestDischargeAsync(long admissionId, RequestDischargeRequest request, CancellationToken ct = default)
    {
        var admission = await db.Admissions.FirstOrDefaultAsync(a => a.Id == admissionId, ct)
            ?? throw new NotFoundException(nameof(Admission), admissionId);
        if (admission.Status != AdmissionStatus.Admitted)
        {
            throw new DomainException("Only an active admission can be discharged.");
        }
        if (admission.DischargeRequestedAt is not null)
        {
            throw new DomainException("Discharge has already been requested for this admission.");
        }
        if (!await db.Doctors.AnyAsync(d => d.Id == request.RequestedByDoctorId, ct))
        {
            throw new NotFoundException(nameof(Doctor), request.RequestedByDoctorId);
        }
        await ClinicalActor.ResolveAttributedDoctorAsync(db, currentUser, request.RequestedByDoctorId, ct);

        admission.DischargeRequestedAt = timeProvider.GetUtcNow();
        admission.DischargeRequestedByDoctorId = request.RequestedByDoctorId;
        admission.DischargeType = request.DischargeType;
        admission.DischargeTypeNotes = request.DischargeTypeNotes;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DischargeClearanceDto>> GetClearancesAsync(long admissionId, CancellationToken ct = default)
    {
        var billBalance = await db.Bills.AsNoTracking().Where(b => b.AdmissionId == admissionId)
            .OrderByDescending(b => b.Id).Select(b => (decimal?)b.BalanceAmount).FirstOrDefaultAsync(ct);

        return await db.DischargeClearances.AsNoTracking().Where(c => c.AdmissionId == admissionId)
            .OrderBy(c => c.ClearanceType)
            .Select(c => new DischargeClearanceDto(c.Id, c.ClearanceType, c.IsCleared,
                c.ClearedByUserId != null ? db.Users.Where(u => u.Id == c.ClearedByUserId).Select(u => u.FullName).FirstOrDefault() : null,
                c.ClearedAt, c.Remarks, c.ClearanceType == ClearanceType.Billing ? billBalance : null))
            .ToListAsync(ct);
    }

    public async Task ClearChecklistItemAsync(long admissionId, ClearanceType clearanceType, ClearChecklistItemRequest request, CancellationToken ct = default)
    {
        // Each department signs off its own item; IPD.DISCHARGE.CLEAR remains an administrator override.
        var specific = clearanceType switch
        {
            ClearanceType.Billing => PermissionRegistry.DischargeClearBilling,
            ClearanceType.Pharmacy => PermissionRegistry.DischargeClearPharmacy,
            ClearanceType.Nursing => PermissionRegistry.DischargeClearNursing,
            ClearanceType.FinalInvestigation => PermissionRegistry.DischargeClearInvestigation,
            _ => PermissionRegistry.DischargeClear,
        };
        if (!await permissions.HasPermissionAsync(currentUser.UserId, specific, ct) &&
            !await permissions.HasPermissionAsync(currentUser.UserId, PermissionRegistry.DischargeClear, ct))
        {
            throw new ForbiddenException($"You are not allowed to give the {clearanceType} clearance.");
        }
        var admission = await db.Admissions.AsNoTracking().FirstOrDefaultAsync(a => a.Id == admissionId, ct)
            ?? throw new NotFoundException(nameof(Admission), admissionId);
        if (admission.Status != AdmissionStatus.Admitted || admission.DischargeRequestedAt is null)
        {
            throw new DomainException("Clearances can only be given after discharge has been requested for an active admission.");
        }
        if (clearanceType == ClearanceType.Billing && await UnsettledBillAsync(admissionId, ct) is { } unsettled)
        {
            throw new DomainException($"Billing clearance needs the IP bill settled first: {unsettled}. Draw the deposit / collect payment, or mark it as a credit bill.");
        }

        var clearance = await db.DischargeClearances.FirstOrDefaultAsync(c => c.AdmissionId == admissionId && c.ClearanceType == clearanceType, ct)
            ?? throw new NotFoundException(nameof(DischargeClearance), admissionId);
        clearance.IsCleared = true;
        clearance.ClearedByUserId = currentUser.UserId;
        clearance.ClearedAt = timeProvider.GetUtcNow();
        clearance.Remarks = request.Remarks;
        await db.SaveChangesAsync(ct);
    }

    public async Task<long> UpsertDischargeSummaryAsync(long admissionId, UpsertDischargeSummaryRequest request, CancellationToken ct = default)
    {
        if (!await db.Admissions.AnyAsync(a => a.Id == admissionId, ct))
        {
            throw new NotFoundException(nameof(Admission), admissionId);
        }
        var summary = await db.DischargeSummaries.FirstOrDefaultAsync(s => s.AdmissionId == admissionId, ct);
        if (summary is { IsApproved: true })
        {
            throw new DomainException("Discharge summary is already approved and can no longer be edited.");
        }
        if (summary is null)
        {
            summary = new DischargeSummary { AdmissionId = admissionId, FinalDiagnosis = request.FinalDiagnosis };
            db.DischargeSummaries.Add(summary);
        }
        summary.FinalDiagnosis = request.FinalDiagnosis;
        summary.CourseInHospital = request.CourseInHospital;
        summary.MedicationsOnDischarge = request.MedicationsOnDischarge;
        summary.FollowUpAdvice = request.FollowUpAdvice;
        summary.ConditionAtDischarge = request.ConditionAtDischarge;
        await db.SaveChangesAsync(ct);
        return summary.Id;
    }

    public async Task<DischargeSummaryDto?> GetDischargeSummaryAsync(long admissionId, CancellationToken ct = default) =>
        await db.DischargeSummaries.AsNoTracking().Where(s => s.AdmissionId == admissionId)
            .Include(s => s.ApprovedByDoctor).ThenInclude(d => d!.User)
            .Select(s => new DischargeSummaryDto(s.Id, s.AdmissionId, s.FinalDiagnosis, s.CourseInHospital,
                s.MedicationsOnDischarge, s.FollowUpAdvice, s.ConditionAtDischarge, s.IsApproved,
                s.ApprovedByDoctor != null ? s.ApprovedByDoctor.User!.FullName : null, s.ApprovedAt))
            .FirstOrDefaultAsync(ct);

    public async Task ApproveDischargeSummaryAsync(long admissionId, ApproveDischargeSummaryRequest request, CancellationToken ct = default)
    {
        var summary = await db.DischargeSummaries.FirstOrDefaultAsync(s => s.AdmissionId == admissionId, ct)
            ?? throw new NotFoundException(nameof(DischargeSummary), admissionId);
        if (summary.IsApproved)
        {
            throw new DomainException("This discharge summary is already approved.");
        }
        if (!await db.Doctors.AnyAsync(d => d.Id == request.ApprovedByDoctorId, ct))
        {
            throw new NotFoundException(nameof(Doctor), request.ApprovedByDoctorId);
        }
        // An e-signature: only a doctor, signing as themselves, who is treating this patient.
        var callerDoctorId = await ClinicalActor.GetCallerDoctorIdAsync(db, currentUser, ct)
            ?? throw new ForbiddenException("Only a doctor can approve (e-sign) a discharge summary.");
        if (callerDoctorId != request.ApprovedByDoctorId)
        {
            throw new ForbiddenException("You can only approve a discharge summary in your own name.");
        }
        var isTreating = await db.Admissions.AnyAsync(a => a.Id == admissionId &&
            (a.PrimaryConsultantId == callerDoctorId || a.Consultants.Any(c => c.DoctorId == callerDoctorId && c.EndedAt == null)), ct);
        if (!isTreating)
        {
            throw new ForbiddenException("Only the treating consultant can approve this patient's discharge summary.");
        }
        summary.IsApproved = true;
        summary.ApprovedByDoctorId = request.ApprovedByDoctorId;
        summary.ApprovedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    public async Task FinalizeDischargeAsync(long admissionId, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var admission = await db.Admissions.Include(a => a.BedAllocations)
                .FirstOrDefaultAsync(a => a.Id == admissionId, token) ?? throw new NotFoundException(nameof(Admission), admissionId);
            if (admission.Status != AdmissionStatus.Admitted)
            {
                throw new DomainException("Admission is not active.");
            }
            if (admission.DischargeRequestedAt is null)
            {
                throw new DomainException("Discharge has not been requested for this admission.");
            }
            var clearances = await db.DischargeClearances.Where(c => c.AdmissionId == admissionId).ToListAsync(token);
            if (clearances.Any(c => !c.IsCleared))
            {
                var pending = string.Join(", ", clearances.Where(c => !c.IsCleared).Select(c => c.ClearanceType));
                throw new DomainException($"Discharge checklist not fully cleared: {pending}.");
            }
            var summary = await db.DischargeSummaries.FirstOrDefaultAsync(s => s.AdmissionId == admissionId, token);
            if (summary is not { IsApproved: true })
            {
                throw new DomainException("Discharge summary must be approved before final discharge.");
            }
            if (await UnsettledBillAsync(admissionId, token) is { } unsettled)
            {
                throw new DomainException($"Final discharge needs the IP bill settled: {unsettled}.");
            }

            var bed = await db.Beds.FirstOrDefaultAsync(b => b.Id == admission.CurrentBedId, token)
                ?? throw new NotFoundException(nameof(Bed), admission.CurrentBedId);
            var now = timeProvider.GetUtcNow();
            var current = admission.BedAllocations.First(ba => ba.ReleasedAt == null);
            current.ReleasedAt = now;
            bed.Status = BedStatus.Vacant;

            admission.Status = AdmissionStatus.Discharged;
            admission.DischargedAt = now;
            await db.SaveChangesAsync(token);
        }, ct);

    public async Task<byte[]> GenerateDischargeSummaryPdfAsync(long admissionId, CancellationToken ct = default)
    {
        var a = await db.Admissions.AsNoTracking().Include(x => x.Patient).Include(x => x.PrimaryConsultant).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(x => x.Id == admissionId, ct) ?? throw new NotFoundException(nameof(Admission), admissionId);
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == a.BranchId, ct);
        var summary = await db.DischargeSummaries.AsNoTracking().Include(s => s.ApprovedByDoctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(s => s.AdmissionId == admissionId, ct)
            ?? throw new DomainException("No discharge summary recorded for this admission.");
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        return pdf.GenerateDischargeSummaryPdf(new DischargeSummaryPdfData(
            a.AdmissionNumber, a.Patient!.FirstName + " " + a.Patient.LastName, a.Patient.Uhid, a.Patient.Gender.ToString(),
            IpdMath.FormatAge(a.Patient.DateOfBirth, a.Patient.EstimatedAge, today), branch?.Name ?? string.Empty, branch?.Address ?? string.Empty,
            a.AdmissionDate, a.DischargedAt ?? timeProvider.GetUtcNow(), a.PrimaryConsultant!.User!.FullName,
            summary.FinalDiagnosis, summary.CourseInHospital, summary.MedicationsOnDischarge, summary.FollowUpAdvice,
            summary.ConditionAtDischarge, summary.ApprovedByDoctor?.User?.FullName, summary.ApprovedAt));
    }

    public async Task<byte[]> GenerateDischargeSlipAsync(long admissionId, CancellationToken ct = default)
    {
        var a = await db.Admissions.AsNoTracking().Include(x => x.Patient).Include(x => x.CurrentBed).ThenInclude(b => b!.Room).ThenInclude(r => r!.Ward)
            .FirstOrDefaultAsync(x => x.Id == admissionId, ct) ?? throw new NotFoundException(nameof(Admission), admissionId);
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == a.BranchId, ct);
        var bill = await db.Bills.AsNoTracking().Where(b => b.AdmissionId == admissionId).OrderByDescending(b => b.Id).FirstOrDefaultAsync(ct);

        return pdf.GenerateDischargeSlip(new DischargeSlipData(
            a.AdmissionNumber, a.Patient!.FirstName + " " + a.Patient.LastName, a.Patient.Uhid, branch?.Name ?? string.Empty,
            a.AdmissionDate, a.DischargedAt ?? timeProvider.GetUtcNow(), (a.DischargeType ?? DischargeType.Normal).ToString(),
            a.CurrentBed!.Room!.Ward!.Name, a.CurrentBed.Room.Name, a.CurrentBed.Code,
            bill?.TotalAmount ?? 0, bill?.BalanceAmount ?? 0));
    }
}
