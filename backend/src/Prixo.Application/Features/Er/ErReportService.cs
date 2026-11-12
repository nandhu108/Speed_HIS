using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Ipd;
using Prixo.Application.Features.Reports;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Er;

/// <summary>
/// "ER Report" — Summary (grouped by arrival date) or Detail (per-visit) breakdown of
/// Emergency/Casualty visits, mirroring OpReportService/IpReportService's shape. ESI level and
/// Disposition live on Triage/EmergencyAssessment (one row per visit each, upserted as the case
/// progresses — see those entities' own doc comments), not on EmergencyVisit itself, so those two
/// filters are applied after bulk-loading the joined rows rather than in the main SQL query.
/// </summary>
public class ErReportService(IAppDbContext db)
{
    public async Task<ReportResultDto> GetErReportAsync(ErReportQuery query, CancellationToken ct = default)
    {
        var mode = (query.Mode ?? "summary").ToLowerInvariant();
        if (mode is not ("summary" or "detail"))
        {
            throw new DomainException($"Unknown report mode '{query.Mode}' — use 'summary' or 'detail'.");
        }

        var visits = db.EmergencyVisits.AsNoTracking()
            .Include(v => v.Patient).ThenInclude(p => p!.PatientCategory)
            .Include(v => v.AssignedDoctor).ThenInclude(d => d!.User)
            .AsQueryable();
        if (query.FromDate is { } from) visits = visits.Where(v => v.ArrivedAt >= AtStart(from));
        if (query.ToDate is { } to) visits = visits.Where(v => v.ArrivedAt < AtStart(to.AddDays(1)));
        if (query.DoctorId is { } doctorId) visits = visits.Where(v => v.AssignedDoctorId == doctorId);
        if (query.Status is { } status) visits = visits.Where(v => v.Status == status);
        if (query.IsMlc is { } isMlc) visits = visits.Where(v => v.IsMlc == isMlc);
        if (query.PatientCategoryId is { } categoryId) visits = visits.Where(v => v.Patient!.PatientCategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(query.RegNo)) visits = visits.Where(v => v.Patient!.Uhid.Contains(query.RegNo));
        if (!string.IsNullOrWhiteSpace(query.PatientName))
        {
            var name = query.PatientName.Trim();
            visits = visits.Where(v => (v.Patient!.FirstName + " " + v.Patient.LastName).Contains(name));
        }

        var rows = await visits.OrderByDescending(v => v.ArrivedAt).ToListAsync(ct);
        var visitIds = rows.Select(v => v.Id).ToList();

        var triageByVisit = (await db.Triages.AsNoTracking()
            .Where(t => visitIds.Contains(t.EmergencyVisitId))
            .OrderByDescending(t => t.TriagedAt)
            .ToListAsync(ct))
            .GroupBy(t => t.EmergencyVisitId)
            .ToDictionary(g => g.Key, g => g.First());

        var assessmentByVisit = (await db.EmergencyAssessments.AsNoTracking()
            .Where(a => visitIds.Contains(a.EmergencyVisitId))
            .OrderByDescending(a => a.AssessedAt)
            .ToListAsync(ct))
            .GroupBy(a => a.EmergencyVisitId)
            .ToDictionary(g => g.Key, g => g.First());

        var mlcByVisit = (await db.MlcRecords.AsNoTracking()
            .Where(m => visitIds.Contains(m.EmergencyVisitId))
            .ToListAsync(ct))
            .GroupBy(m => m.EmergencyVisitId)
            .ToDictionary(g => g.Key, g => g.First());

        if (query.EsiLevel is { } esiLevel)
        {
            rows = rows.Where(v => triageByVisit.TryGetValue(v.Id, out var t) && t.EsiLevel == esiLevel).ToList();
        }
        if (query.Disposition is { } disposition)
        {
            rows = rows.Where(v => assessmentByVisit.TryGetValue(v.Id, out var a) && a.Disposition == disposition).ToList();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (mode == "detail")
        {
            IReadOnlyList<ReportColumnDto> detailColumns =
            [
                new("arrivedAt", "Arrived At"), new("visitNumber", "Visit No"), new("tokenNumber", "Token"),
                new("patientUhid", "UHID"), new("patientName", "Patient"), new("age", "Age"), new("gender", "Gender"),
                new("patientType", "Patient Type"), new("arrivalMode", "Arrival Mode"), new("esiLevel", "ESI Level"),
                new("chiefComplaint", "Chief Complaint"), new("assignedDoctor", "Doctor"), new("status", "Status"),
                new("disposition", "Disposition"), new("isMlc", "MLC"), new("mlcNumber", "MLC No"),
            ];
            var detailRows = rows.Select(v =>
            {
                var triage = triageByVisit.GetValueOrDefault(v.Id);
                var assessment = assessmentByVisit.GetValueOrDefault(v.Id);
                var mlc = mlcByVisit.GetValueOrDefault(v.Id);
                return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
                {
                    ["arrivedAt"] = v.ArrivedAt.ToString("yyyy-MM-dd HH:mm"),
                    ["visitNumber"] = v.VisitNumber,
                    ["tokenNumber"] = v.TokenNumber,
                    ["patientUhid"] = v.Patient!.Uhid,
                    ["patientName"] = v.Patient.FirstName + " " + v.Patient.LastName,
                    ["age"] = IpdMath.FormatAge(v.Patient.DateOfBirth, v.Patient.EstimatedAge, today),
                    ["gender"] = v.Patient.Gender.ToString(),
                    ["patientType"] = v.Patient.PatientCategory?.Name ?? "-",
                    ["arrivalMode"] = v.ArrivalMode.ToString(),
                    ["esiLevel"] = triage?.EsiLevel.ToString() ?? "-",
                    ["chiefComplaint"] = triage?.ChiefComplaint ?? "-",
                    ["assignedDoctor"] = v.AssignedDoctor?.User?.FullName ?? "-",
                    ["status"] = v.Status.ToString(),
                    ["disposition"] = assessment?.Disposition?.ToString() ?? "-",
                    ["isMlc"] = v.IsMlc ? "Yes" : "No",
                    ["mlcNumber"] = mlc?.MlcNumber ?? "-",
                };
            }).ToList();
            return new ReportResultDto("ER Report (Detail)", detailColumns, detailRows);
        }

        var dispositionByVisitId = rows.ToDictionary(v => v.Id, v => assessmentByVisit.GetValueOrDefault(v.Id)?.Disposition);

        IReadOnlyList<ReportColumnDto> summaryColumns =
        [
            new("visitDate", "Date"), new("totalVisits", "Total Visits"), new("mlcCount", "MLC"),
            new("admittedCount", "Admitted"), new("dischargedCount", "Discharged"), new("lamaCount", "LAMA"),
            new("broughtDeadCount", "Brought Dead"), new("transferredCount", "Transferred Out"),
        ];
        var summaryRows = rows
            .GroupBy(v => DateOnly.FromDateTime(v.ArrivedAt.UtcDateTime))
            .OrderBy(g => g.Key)
            .Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["visitDate"] = g.Key.ToString("yyyy-MM-dd"),
                ["totalVisits"] = g.Count(),
                ["mlcCount"] = g.Count(v => v.IsMlc),
                ["admittedCount"] = g.Count(v => dispositionByVisitId[v.Id] == DispositionType.AdmittedConverted),
                ["dischargedCount"] = g.Count(v => dispositionByVisitId[v.Id] == DispositionType.Discharged),
                ["lamaCount"] = g.Count(v => dispositionByVisitId[v.Id] == DispositionType.Lama),
                ["broughtDeadCount"] = g.Count(v => dispositionByVisitId[v.Id] == DispositionType.BroughtDead),
                ["transferredCount"] = g.Count(v => dispositionByVisitId[v.Id] == DispositionType.TransferredOut),
            }).ToList();
        return new ReportResultDto("ER Report (Summary)", summaryColumns, summaryRows);
    }

    private static DateTimeOffset AtStart(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
