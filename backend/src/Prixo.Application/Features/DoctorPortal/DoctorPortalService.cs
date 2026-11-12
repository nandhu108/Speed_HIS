using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Ipd;
using Prixo.Application.Features.Lab;
using Prixo.Application.Features.Opd;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.DoctorPortal;

/// <summary>
/// Doctor Portal (plan §22: "same SSO, doctor layout") — unlike the Patient Portal, this
/// reuses the staff JWT/RBAC outright (a doctor already has a staff User account); every
/// method here just resolves "my own Doctor profile" from the caller's UserId and reuses
/// the exact staff services/queries the corresponding staff screens use — no parallel
/// queue engine, discharge-approval engine, or lab-order engine.
/// </summary>
public class DoctorPortalService(
    IAppDbContext db, ICurrentUser currentUser, OpVisitService opVisits, DischargeService discharge)
{
    private async Task<Doctor> GetMyDoctorAsync(CancellationToken ct) =>
        await db.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == currentUser.UserId, ct)
            ?? throw new DomainException("Your account is not linked to a doctor profile.");

    public async Task<DoctorDashboardDto> GetDashboardAsync(DateOnly today, CancellationToken ct = default)
    {
        var doctor = await GetMyDoctorAsync(ct);

        var todayOpCount = await db.OpVisits.AsNoTracking()
            .CountAsync(v => v.DoctorId == doctor.Id && v.VisitDate == today, ct);

        var activeInpatients = await db.Admissions.AsNoTracking()
            .CountAsync(a => a.Status == AdmissionStatus.Admitted &&
                (a.PrimaryConsultantId == doctor.Id || a.Consultants.Any(c => c.DoctorId == doctor.Id && c.EndedAt == null)), ct);

        var pendingDischargeSigns = await db.DischargeSummaries.AsNoTracking()
            .CountAsync(s => !s.IsApproved && s.Admission!.Status == AdmissionStatus.Admitted &&
                (s.Admission.PrimaryConsultantId == doctor.Id || s.Admission.Consultants.Any(c => c.DoctorId == doctor.Id && c.EndedAt == null)), ct);

        var unacknowledgedCriticalAlerts = await db.CriticalAlerts.AsNoTracking()
            .CountAsync(a => !a.IsAcknowledged && a.LabResult!.LabOrderItem!.LabOrder!.OrderedByDoctorId == doctor.Id, ct);

        return new DoctorDashboardDto(todayOpCount, activeInpatients, pendingDischargeSigns, unacknowledgedCriticalAlerts);
    }

    public async Task<IReadOnlyList<QueueEntryDto>> GetMyQueueAsync(DateOnly date, CancellationToken ct = default)
    {
        var doctor = await GetMyDoctorAsync(ct);
        return await opVisits.GetQueueAsync(doctor.Id, date, ct);
    }

    public async Task<IReadOnlyList<AdmissionListItemDto>> GetMyInpatientsAsync(CancellationToken ct = default)
    {
        var doctor = await GetMyDoctorAsync(ct);
        return await db.Admissions.AsNoTracking()
            .Where(a => a.Status == AdmissionStatus.Admitted &&
                (a.PrimaryConsultantId == doctor.Id || a.Consultants.Any(c => c.DoctorId == doctor.Id && c.EndedAt == null)))
            .OrderByDescending(a => a.AdmissionDate)
            .Select(a => new AdmissionListItemDto(
                a.Id, a.AdmissionNumber, a.AdmissionDate, a.PatientId, a.Patient!.Uhid,
                a.Patient!.FirstName + " " + a.Patient!.LastName, a.AdmissionType, a.Status,
                a.PrimaryConsultant!.User!.FullName, a.CurrentBed!.Room!.Ward!.Name, a.CurrentBed.Room.Name, a.CurrentBed.Code))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LabOrderListItemDto>> GetMyOrdersAsync(CancellationToken ct = default)
    {
        var doctor = await GetMyDoctorAsync(ct);
        return await db.LabOrders.AsNoTracking()
            .Where(o => o.OrderedByDoctorId == doctor.Id)
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id)
            .Take(100)
            .Select(o => new LabOrderListItemDto(
                o.Id, o.OrderNumber, o.OrderDate, o.PatientId, o.Patient!.Uhid,
                o.Patient!.FirstName + " " + o.Patient!.LastName, o.Priority, o.Status, o.Items.Count))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PendingDischargeSummaryDto>> GetMyPendingDischargeSummariesAsync(CancellationToken ct = default)
    {
        var doctor = await GetMyDoctorAsync(ct);
        return await db.DischargeSummaries.AsNoTracking()
            .Where(s => !s.IsApproved && s.Admission!.Status == AdmissionStatus.Admitted &&
                (s.Admission.PrimaryConsultantId == doctor.Id || s.Admission.Consultants.Any(c => c.DoctorId == doctor.Id && c.EndedAt == null)))
            .OrderBy(s => s.Admission!.DischargeRequestedAt)
            .Select(s => new PendingDischargeSummaryDto(
                s.AdmissionId, s.Admission!.AdmissionNumber, s.Admission.PatientId, s.Admission.Patient!.Uhid,
                s.Admission.Patient!.FirstName + " " + s.Admission.Patient!.LastName, s.FinalDiagnosis))
            .ToListAsync(ct);
    }

    /// <summary>Ownership-checked wrapper over DischargeService.ApproveDischargeSummaryAsync — a doctor may only e-sign summaries for admissions where they're a consultant.</summary>
    public async Task ApproveMyDischargeSummaryAsync(long admissionId, CancellationToken ct = default)
    {
        var doctor = await GetMyDoctorAsync(ct);
        var isMyPatient = await db.Admissions.AsNoTracking().AnyAsync(a => a.Id == admissionId &&
            (a.PrimaryConsultantId == doctor.Id || a.Consultants.Any(c => c.DoctorId == doctor.Id && c.EndedAt == null)), ct);
        if (!isMyPatient)
        {
            throw new DomainException("You are not a consultant for this admission.");
        }
        await discharge.ApproveDischargeSummaryAsync(admissionId, new ApproveDischargeSummaryRequest(doctor.Id), ct);
    }

    /// <summary>"Doctor Opinion Entry" inbox — every Internal cross-consult referral addressed to
    /// me, across all patients/visits.</summary>
    public async Task<IReadOnlyList<OpinionRequestDto>> GetMyOpinionRequestsAsync(CancellationToken ct = default)
    {
        var doctor = await GetMyDoctorAsync(ct);
        return await opVisits.GetOpinionRequestsForDoctorAsync(doctor.Id, ct);
    }

    /// <summary>Ownership-checked wrapper over OpVisitService.SaveOpinionAsync — a doctor may only
    /// enter an opinion for a cross-consult referral addressed to them.</summary>
    public async Task SaveMyOpinionAsync(long referralId, SaveReferralOpinionRequest request, CancellationToken ct = default)
    {
        var doctor = await GetMyDoctorAsync(ct);
        var referral = await db.VisitReferrals.AsNoTracking().FirstOrDefaultAsync(r => r.Id == referralId, ct)
            ?? throw new NotFoundException(nameof(VisitReferral), referralId);
        if (referral.ReferredToDoctorId != doctor.Id)
        {
            throw new DomainException("This opinion request is not addressed to you.");
        }

        await opVisits.SaveOpinionAsync(referral.VisitId, referralId, request, ct);
    }
}
