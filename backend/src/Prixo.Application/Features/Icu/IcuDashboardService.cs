using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Icu;

/// <summary>Census/ventilated-count/severity aggregation (plan §15's "ICU Dashboard") — every admission currently on an ICU-flagged ward (Ward.IsIcu, set by IPD's bed board), no separate "ICU stay" entity.</summary>
public class IcuDashboardService(IAppDbContext db)
{
    public async Task<IcuDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var admissions = await db.Admissions.AsNoTracking()
            .Where(a => a.Status == AdmissionStatus.Admitted && a.CurrentBed!.Room!.Ward!.IsIcu)
            .Include(a => a.Patient)
            .Include(a => a.CurrentBed).ThenInclude(b => b!.Room).ThenInclude(r => r!.Ward)
            .Select(a => new
            {
                a.Id,
                a.AdmissionNumber,
                PatientName = a.Patient!.FirstName + " " + a.Patient.LastName,
                WardName = a.CurrentBed!.Room!.Ward!.Name,
                RoomName = a.CurrentBed.Room.Name,
                BedCode = a.CurrentBed.Code,
            })
            .ToListAsync(ct);

        var admissionIds = admissions.Select(a => a.Id).ToList();

        var ventilatedIds = await db.VentilatorSessions.AsNoTracking()
            .Where(s => admissionIds.Contains(s.AdmissionId) && s.StoppedAt == null)
            .Select(s => s.AdmissionId).Distinct().ToListAsync(ct);

        var latestSeverity = await db.IcuSeverityAssessments.AsNoTracking()
            .Where(s => admissionIds.Contains(s.AdmissionId))
            .GroupBy(s => s.AdmissionId)
            .Select(g => new { AdmissionId = g.Key, Level = g.OrderByDescending(s => s.AssessedAt).First().SeverityLevel })
            .ToListAsync(ct);
        var severityByAdmission = latestSeverity.ToDictionary(x => x.AdmissionId, x => (IcuSeverityLevel?)x.Level);

        var alertCounts = await db.IcuVitalsAlerts.AsNoTracking()
            .Where(a => admissionIds.Contains(a.AdmissionId) && !a.IsAcknowledged)
            .GroupBy(a => a.AdmissionId)
            .Select(g => new { AdmissionId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var alertCountByAdmission = alertCounts.ToDictionary(x => x.AdmissionId, x => x.Count);

        var patients = admissions.Select(a => new IcuCensusPatientDto(
            a.Id, a.AdmissionNumber, a.PatientName, a.WardName, a.RoomName, a.BedCode,
            ventilatedIds.Contains(a.Id),
            severityByAdmission.GetValueOrDefault(a.Id),
            alertCountByAdmission.GetValueOrDefault(a.Id, 0))).ToList();

        return new IcuDashboardDto(patients.Count, ventilatedIds.Count, patients);
    }
}
