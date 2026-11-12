using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Ipd;
using Prixo.Domain.Entities.Icu;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Icu;

/// <summary>
/// ICU's own vitals-recording entry point — writes the exact same IpVitals row IPD's
/// ClinicalChartService.AddVitalsAsync does (duplicated directly rather than calling into
/// IPD's service, since no Application feature anywhere calls into a sibling feature's
/// service — see OT's OtUsageService for the same "reach into the entity/db directly, not a
/// cross-service call" posture), and additionally evaluates every active CriticalAlertRule
/// against the new reading, raising an IcuVitalsAlert per breach.
/// </summary>
public class IcuVitalsService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<IpVitalsDto> RecordVitalsAsync(long admissionId, AddVitalsRequest request, CancellationToken ct = default)
    {
        if (!await db.Admissions.AnyAsync(a => a.Id == admissionId, ct))
        {
            throw new NotFoundException(nameof(Admission), admissionId);
        }

        var branchId = RequireBranch();
        var now = timeProvider.GetUtcNow();
        var vitals = new IpVitals
        {
            AdmissionId = admissionId, RecordedAt = now, RecordedByUserId = currentUser.UserId,
            TemperatureF = request.TemperatureF, PulseRate = request.PulseRate, RespiratoryRate = request.RespiratoryRate,
            BloodPressureSystolic = request.BloodPressureSystolic, BloodPressureDiastolic = request.BloodPressureDiastolic,
            SpO2 = request.SpO2, RandomBloodSugar = request.RandomBloodSugar,
            GlasgowComaScale = request.GlasgowComaScale, MeanArterialPressure = request.MeanArterialPressure,
            CentralVenousPressure = request.CentralVenousPressure, UrineOutputMl = request.UrineOutputMl,
        };
        db.IpVitals.Add(vitals);
        await db.SaveChangesAsync(ct);

        var snapshot = new VitalsSnapshot(
            vitals.TemperatureF, vitals.PulseRate, vitals.RespiratoryRate,
            vitals.BloodPressureSystolic, vitals.BloodPressureDiastolic, vitals.SpO2);
        var rules = await db.CriticalAlertRules.AsNoTracking().Where(r => r.BranchId == branchId && r.IsActive).ToListAsync(ct);
        foreach (var rule in rules)
        {
            var value = IcuMath.ExtractValue(rule.Parameter, snapshot);
            if (value is { } v && IcuMath.IsBreached(rule.Operator, v, rule.ThresholdValue))
            {
                db.IcuVitalsAlerts.Add(new IcuVitalsAlert
                {
                    BranchId = branchId, AdmissionId = admissionId, IpVitalsId = vitals.Id,
                    CriticalAlertRuleId = rule.Id, RaisedAt = now,
                });
            }
        }
        await db.SaveChangesAsync(ct);

        var recordedByName = await db.Users.Where(u => u.Id == vitals.RecordedByUserId).Select(u => u.FullName).FirstAsync(ct);
        return new IpVitalsDto(
            vitals.Id, vitals.RecordedAt, recordedByName, vitals.TemperatureF, vitals.PulseRate, vitals.RespiratoryRate,
            vitals.BloodPressureSystolic, vitals.BloodPressureDiastolic, vitals.SpO2, vitals.RandomBloodSugar,
            vitals.GlasgowComaScale, vitals.MeanArterialPressure, vitals.CentralVenousPressure, vitals.UrineOutputMl);
    }

    public async Task<IReadOnlyList<IcuVitalsAlertDto>> GetAlertsAsync(bool onlyUnacknowledged, CancellationToken ct = default)
    {
        var alerts = db.IcuVitalsAlerts.AsNoTracking();
        if (onlyUnacknowledged) alerts = alerts.Where(a => !a.IsAcknowledged);

        var rows = await alerts.Include(a => a.Admission).ThenInclude(ad => ad!.Patient)
            .Include(a => a.IpVitals).Include(a => a.CriticalAlertRule).Include(a => a.AcknowledgedByUser)
            .OrderByDescending(a => a.RaisedAt)
            .ToListAsync(ct);

        return rows.Select(a =>
        {
            var snapshot = new VitalsSnapshot(
                a.IpVitals!.TemperatureF, a.IpVitals.PulseRate, a.IpVitals.RespiratoryRate,
                a.IpVitals.BloodPressureSystolic, a.IpVitals.BloodPressureDiastolic, a.IpVitals.SpO2);
            return new IcuVitalsAlertDto(
                a.Id, a.AdmissionId, a.Admission!.Patient!.FirstName + " " + a.Admission.Patient.LastName, a.IpVitalsId,
                a.CriticalAlertRule!.Parameter, IcuMath.ExtractValue(a.CriticalAlertRule.Parameter, snapshot) ?? 0,
                a.CriticalAlertRule.ThresholdValue, a.CriticalAlertRule.Operator, a.CriticalAlertRule.Severity,
                a.RaisedAt, a.IsAcknowledged, a.AcknowledgedByUser != null ? a.AcknowledgedByUser.FullName : null,
                a.AcknowledgedAt, a.AcknowledgementNotes);
        }).ToList();
    }

    public async Task AcknowledgeAlertAsync(long id, AcknowledgeAlertRequest request, CancellationToken ct = default)
    {
        var alert = await db.IcuVitalsAlerts.FirstOrDefaultAsync(a => a.Id == id, ct) ?? throw new NotFoundException(nameof(IcuVitalsAlert), id);
        if (alert.IsAcknowledged)
        {
            throw new DomainException("This alert is already acknowledged.");
        }
        alert.IsAcknowledged = true;
        alert.AcknowledgedByUserId = currentUser.UserId;
        alert.AcknowledgedAt = timeProvider.GetUtcNow();
        alert.AcknowledgementNotes = request.Notes;
        await db.SaveChangesAsync(ct);
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
