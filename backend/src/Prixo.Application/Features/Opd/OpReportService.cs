using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Ipd;
using Prixo.Application.Features.Reports;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Opd;

/// <summary>
/// The OPD report engine — every standalone, date-range-filterable ad-hoc report over
/// `opd.op_visits`/`opd.referrals`, exported via the shared <see cref="ReportExportHelper"/>.
/// Distinct from the day/doctor-only OP_CENSUS row in the Custom Report Builder catalog. Global
/// EF query filters already scope by branch — no manual branch filtering needed here, matching
/// MrdReportService/BiomedReportService. MHC-scoped reports (Category == Mhc) live here too
/// rather than a parallel service — same table, same engine, just a different Category filter
/// and permission at the controller (MhcReportsController vs OpdReportsController).
/// </summary>
public class OpReportService(IAppDbContext db)
{
    public async Task<ReportResultDto> GetVisitsReportAsync(OpReportQuery query, CancellationToken ct = default)
    {
        var mode = (query.Mode ?? "summary").ToLowerInvariant();
        if (mode is not ("summary" or "detail"))
        {
            throw new DomainException($"Unknown report mode '{query.Mode}' — use 'summary' or 'detail'.");
        }

        // Category == Opd only: MHC visits have DoctorId == null, which would throw a
        // NullReferenceException on v.Doctor!.User!.FullName below if they leaked in here.
        var visits = db.OpVisits.AsNoTracking().Where(v => v.Category == VisitCategory.Opd)
            .Include(v => v.Patient).ThenInclude(p => p!.PatientCategory)
            .Include(v => v.Patient).ThenInclude(p => p!.Referrer)
            .Include(v => v.Doctor).ThenInclude(d => d!.User)
            .Include(v => v.Doctor).ThenInclude(d => d!.DoctorDepartments).ThenInclude(dd => dd.Department)
            .Include(v => v.Camp)
            .AsQueryable();
        if (query.FromDate is { } from) visits = visits.Where(v => v.VisitDate >= from);
        if (query.ToDate is { } to) visits = visits.Where(v => v.VisitDate <= to);
        if (query.CampId is { } campId) visits = visits.Where(v => v.CampId == campId);
        if (query.IsMlcCase is { } isMlcCase) visits = visits.Where(v => v.IsMlcCase == isMlcCase);
        if (query.DoctorId is { } doctorId) visits = visits.Where(v => v.DoctorId == doctorId);
        if (query.VisitType is { } visitType) visits = visits.Where(v => v.VisitType == visitType);
        if (query.PatientCategoryId is { } categoryId) visits = visits.Where(v => v.Patient!.PatientCategoryId == categoryId);
        if (query.DepartmentId is { } departmentId) visits = visits.Where(v => v.Doctor!.DoctorDepartments.Any(dd => dd.DepartmentId == departmentId));
        if (query.ReferrerId is { } referrerId) visits = visits.Where(v => v.Patient!.ReferrerId == referrerId);
        if (!string.IsNullOrWhiteSpace(query.RegNo)) visits = visits.Where(v => v.Patient!.Uhid.Contains(query.RegNo));
        if (!string.IsNullOrWhiteSpace(query.PatientName))
        {
            var name = query.PatientName.Trim();
            visits = visits.Where(v => (v.Patient!.FirstName + " " + v.Patient.LastName).Contains(name));
        }
        if (query.ReferralDoctorId is { } referralDoctorId)
        {
            var referredVisitIds = await db.VisitReferrals.AsNoTracking()
                .Where(r => r.ReferredToDoctorId == referralDoctorId)
                .Select(r => r.VisitId)
                .ToListAsync(ct);
            visits = visits.Where(v => referredVisitIds.Contains(v.Id));
        }

        var rows = await visits.OrderBy(v => v.VisitDate).ThenBy(v => v.TokenNumber).ToListAsync(ct);

        if (mode == "detail")
        {
            return await BuildDetailReportAsync(rows, ct);
        }

        IReadOnlyList<ReportColumnDto> summaryColumns =
        [
            new("visitDate", "Date"), new("doctorName", "Doctor"), new("newCount", "New"),
            new("reviewCount", "Review"), new("totalCount", "Total"), new("totalFee", "Total Fee"),
        ];
        var summaryRows = rows
            .GroupBy(v => (v.VisitDate, DoctorName: v.Doctor!.User!.FullName))
            .OrderBy(g => g.Key.VisitDate).ThenBy(g => g.Key.DoctorName)
            .Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["visitDate"] = g.Key.VisitDate.ToString("yyyy-MM-dd"),
                ["doctorName"] = g.Key.DoctorName,
                ["newCount"] = g.Count(v => v.VisitType == Domain.Enums.VisitType.New),
                ["reviewCount"] = g.Count(v => v.VisitType == Domain.Enums.VisitType.Review),
                ["totalCount"] = g.Count(),
                ["totalFee"] = g.Sum(v => v.ConsultationFee),
            }).ToList();
        return new ReportResultDto("OP Report (Summary)", summaryColumns, summaryRows);
    }

    /// <summary>
    /// Builds the OP Report detail rows plus everything resolvable from existing schema that a
    /// front-office "give me everything" report wants beyond the visit row itself: ABHA ID,
    /// department/unit (both resolved through the visit's doctor, since neither lives on OpVisit
    /// directly), the latest referral raised on the visit, the visit's bill status, and who
    /// created the visit. Each is bulk-loaded by id set rather than per-row, to keep this an
    /// O(1)-query-per-lookup-type report regardless of row count. Deliberately does not include
    /// Corporate/Corporate Type/Religion/Area/Case Type or referral-doctor city/address/status
    /// — none of those exist anywhere in the schema yet (not on Patient, OpVisit, or Referrer).
    /// MLC *does* now exist (`OpVisit.IsMlcCase`, added in the Phase 24 registration gap-fill)
    /// and is included below — see "OP MLC Report" and
    /// docs/PATIENT_JOURNEY_GAP_ANALYSIS.md §3.2 for why this stays a plain filter/column on
    /// the existing OP Report rather than feeding Emergency's separate statutory MLC register.
    /// </summary>
    private async Task<ReportResultDto> BuildDetailReportAsync(List<Domain.Entities.Opd.OpVisit> rows, CancellationToken ct)
    {
        var visitIds = rows.Select(v => v.Id).ToList();
        var patientIds = rows.Select(v => v.PatientId).Distinct().ToList();
        var doctorIds = rows.Where(v => v.DoctorId != null).Select(v => v.DoctorId!.Value).Distinct().ToList();
        var creatorIds = rows.Select(v => v.CreatedBy).Distinct().ToList();

        var abhaByPatient = (await db.PatientIdentifiers.AsNoTracking()
            .Where(i => patientIds.Contains(i.PatientId) && i.Type == PatientIdentifierType.Abha)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct))
            .GroupBy(i => i.PatientId)
            .ToDictionary(g => g.Key, g => g.First().Value);

        var latestReferralByVisit = (await db.VisitReferrals.AsNoTracking()
            .Where(r => visitIds.Contains(r.VisitId))
            .Include(r => r.ReferredToDoctor).ThenInclude(d => d!.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct))
            .GroupBy(r => r.VisitId)
            .ToDictionary(g => g.Key, g => g.First());

        var unitNamesByDoctor = (await db.UnitDoctors.AsNoTracking()
            .Where(ud => doctorIds.Contains(ud.DoctorId))
            .Include(ud => ud.Unit)
            .ToListAsync(ct))
            .GroupBy(ud => ud.DoctorId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(ud => ud.Unit!.Name).Distinct()));

        var billStatusByVisit = (await db.Bills.AsNoTracking()
            .Where(b => b.OpVisitId != null && visitIds.Contains(b.OpVisitId!.Value))
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct))
            .GroupBy(b => b.OpVisitId!.Value)
            .ToDictionary(g => g.Key, g => g.First().Status.ToString());

        var userNameById = await db.Users.AsNoTracking()
            .Where(u => creatorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("visitDate", "Date"), new("visitNumber", "Visit No"), new("tokenNumber", "Token"),
            new("patientUhid", "UHID"), new("abhaId", "ABHA ID"), new("patientName", "Patient"),
            new("age", "Age"), new("gender", "Gender"), new("visitType", "Visit Type"), new("status", "Status"),
            new("department", "Department"), new("doctorName", "Doctor"), new("referralDoctor", "Referral Doctor"),
            new("referredSource", "Referred Source"), new("referredSourceName", "Referred Source Name"),
            new("patientType", "Patient Type"), new("unit", "Unit"), new("mobile", "Mobile"),
            new("address", "Address"), new("email", "Email"), new("nationality", "Nationality"),
            new("occupation", "Occupation"), new("camp", "Camp"), new("isMlc", "MLC"), new("billStatus", "Bill Status"),
            new("consultationFee", "Fee"), new("createdUser", "Created By"),
        ];

        var detailRows = rows.Select(v =>
        {
            var referral = latestReferralByVisit.GetValueOrDefault(v.Id);
            var referralDoctorName = referral is null
                ? "-"
                : referral.Direction == ReferralDirection.Internal
                    ? referral.ReferredToDoctor?.User?.FullName ?? "-"
                    : referral.ExternalDetails ?? "-";
            var deptNames = v.Doctor?.DoctorDepartments.Select(dd => dd.Department!.Name).Distinct().ToList() ?? [];
            var addressParts = new[] { v.Patient!.Address, v.Patient.City?.Name }.Where(s => !string.IsNullOrWhiteSpace(s));

            return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["visitDate"] = v.VisitDate.ToString("yyyy-MM-dd"),
                ["visitNumber"] = v.VisitNumber,
                ["tokenNumber"] = v.TokenNumber,
                ["patientUhid"] = v.Patient.Uhid,
                ["abhaId"] = abhaByPatient.GetValueOrDefault(v.PatientId, "-"),
                ["patientName"] = v.Patient.FirstName + " " + v.Patient.LastName,
                ["age"] = IpdMath.FormatAge(v.Patient.DateOfBirth, v.Patient.EstimatedAge, today),
                ["gender"] = v.Patient.Gender.ToString(),
                ["visitType"] = v.VisitType.ToString(),
                ["status"] = v.Status.ToString(),
                ["department"] = deptNames.Count > 0 ? string.Join(", ", deptNames) : "-",
                ["doctorName"] = v.Doctor?.User?.FullName ?? "-",
                ["referralDoctor"] = referralDoctorName,
                ["referredSource"] = v.Patient.Referrer?.Type.ToString() ?? "-",
                ["referredSourceName"] = v.Patient.Referrer?.Name ?? "-",
                ["patientType"] = v.Patient.PatientCategory?.Name ?? "-",
                ["unit"] = v.DoctorId != null ? unitNamesByDoctor.GetValueOrDefault(v.DoctorId.Value, "-") : "-",
                ["mobile"] = v.Patient.Mobile,
                ["address"] = string.Join(", ", addressParts),
                ["email"] = v.Patient.Email ?? "-",
                ["nationality"] = v.Patient.Nationality?.Name,
                ["occupation"] = v.Patient.Occupation ?? "-",
                ["camp"] = v.Camp?.Name ?? "-",
                ["isMlc"] = v.IsMlcCase ? "Yes" : "No",
                ["billStatus"] = billStatusByVisit.GetValueOrDefault(v.Id, "No Bill"),
                ["consultationFee"] = v.ConsultationFee,
                ["createdUser"] = userNameById.GetValueOrDefault(v.CreatedBy, "-"),
            };
        }).ToList();
        return new ReportResultDto("OP Report (Detail)", columns, detailRows);
    }

    /// <summary>"OP Cancel Report" — every OPD visit cancelled in the range.</summary>
    public async Task<ReportResultDto> GetCancelReportAsync(ReportExecuteRequest query, CancellationToken ct = default)
    {
        var visits = db.OpVisits.AsNoTracking()
            .Where(v => v.Category == VisitCategory.Opd && v.Status == OpVisitStatus.Cancelled)
            .Include(v => v.Patient).Include(v => v.Doctor).ThenInclude(d => d!.User).AsQueryable();
        if (query.FromDate is { } from) visits = visits.Where(v => v.VisitDate >= from);
        if (query.ToDate is { } to) visits = visits.Where(v => v.VisitDate <= to);

        var rows = await visits.OrderByDescending(v => v.CancelledAt).ToListAsync(ct);

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("visitDate", "Date"), new("visitNumber", "Visit No"), new("patientName", "Patient"),
            new("patientUhid", "UHID"), new("doctorName", "Doctor"), new("cancelledAt", "Cancelled At"),
            new("cancelReason", "Reason"),
        ];
        var reportRows = rows.Select(v => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["visitDate"] = v.VisitDate.ToString("yyyy-MM-dd"),
            ["visitNumber"] = v.VisitNumber,
            ["patientName"] = v.Patient!.FirstName + " " + v.Patient.LastName,
            ["patientUhid"] = v.Patient.Uhid,
            ["doctorName"] = v.Doctor!.User!.FullName,
            ["cancelledAt"] = v.CancelledAt?.ToString("yyyy-MM-dd HH:mm"),
            ["cancelReason"] = v.CancelReason,
        }).ToList();
        return new ReportResultDto("OP Cancel Report", columns, reportRows);
    }

    /// <summary>"OP TAT Report" — registration→consult-start and consult-start→completion
    /// turnaround, computed from timestamps `OpVisitService` already stamps (`CreatedAt`,
    /// `InConsultAt`, `CompletedAt`) — no new columns.</summary>
    public async Task<ReportResultDto> GetTatReportAsync(OpTatReportQuery query, CancellationToken ct = default)
    {
        var visits = db.OpVisits.AsNoTracking()
            .Where(v => v.Category == VisitCategory.Opd && v.Status == OpVisitStatus.Completed
                && v.InConsultAt != null && v.CompletedAt != null)
            .Include(v => v.Patient).Include(v => v.Doctor).ThenInclude(d => d!.User).AsQueryable();
        if (query.FromDate is { } from) visits = visits.Where(v => v.VisitDate >= from);
        if (query.ToDate is { } to) visits = visits.Where(v => v.VisitDate <= to);
        if (query.DoctorId is { } doctorId) visits = visits.Where(v => v.DoctorId == doctorId);

        var rows = await visits.OrderByDescending(v => v.VisitDate).ThenByDescending(v => v.TokenNumber).ToListAsync(ct);

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("visitDate", "Date"), new("visitNumber", "Visit No"), new("patientName", "Patient"),
            new("doctorName", "Doctor"), new("registeredAt", "Registered"), new("consultStartAt", "Consult Start"),
            new("completedAt", "Completed"), new("waitMinutes", "Wait (min)"), new("consultMinutes", "Consult (min)"),
            new("totalMinutes", "Total TAT (min)"),
        ];
        var reportRows = rows.Select(v =>
        {
            var wait = (v.InConsultAt!.Value - v.CreatedAt).TotalMinutes;
            var consult = (v.CompletedAt!.Value - v.InConsultAt.Value).TotalMinutes;
            return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["visitDate"] = v.VisitDate.ToString("yyyy-MM-dd"),
                ["visitNumber"] = v.VisitNumber,
                ["patientName"] = v.Patient!.FirstName + " " + v.Patient.LastName,
                ["doctorName"] = v.Doctor!.User!.FullName,
                ["registeredAt"] = v.CreatedAt.ToString("HH:mm"),
                ["consultStartAt"] = v.InConsultAt.Value.ToString("HH:mm"),
                ["completedAt"] = v.CompletedAt.Value.ToString("HH:mm"),
                ["waitMinutes"] = Math.Round(wait, 1),
                ["consultMinutes"] = Math.Round(consult, 1),
                ["totalMinutes"] = Math.Round(wait + consult, 1),
            };
        }).ToList();
        return new ReportResultDto("OP TAT Report", columns, reportRows);
    }

    /// <summary>"Referral Report" — every Internal/External referral raised across OPD visits,
    /// with the cross-consult opinion status (see `docs/modules/opd.md`'s "Doctor Opinion
    /// Entry" section) for Internal rows.</summary>
    public async Task<ReportResultDto> GetReferralReportAsync(ReferralReportQuery query, CancellationToken ct = default)
    {
        var referrals = db.VisitReferrals.AsNoTracking()
            .Where(r => r.Visit!.Category == VisitCategory.Opd)
            .Include(r => r.Visit).ThenInclude(v => v!.Patient)
            .Include(r => r.Visit).ThenInclude(v => v!.Doctor).ThenInclude(d => d!.User)
            .Include(r => r.ReferredToDoctor).ThenInclude(d => d!.User)
            .AsQueryable();
        if (query.FromDate is { } from) referrals = referrals.Where(r => r.Visit!.VisitDate >= from);
        if (query.ToDate is { } to) referrals = referrals.Where(r => r.Visit!.VisitDate <= to);
        if (query.Direction is { } direction) referrals = referrals.Where(r => r.Direction == direction);

        var rows = await referrals.OrderByDescending(r => r.Visit!.VisitDate).ToListAsync(ct);

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("visitDate", "Date"), new("visitNumber", "Visit No"), new("patientName", "Patient"),
            new("referringDoctor", "Referring Doctor"), new("direction", "Direction"), new("referredTo", "Referred To"),
            new("reason", "Reason"), new("opinionStatus", "Opinion Status"),
        ];
        var reportRows = rows.Select(r => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["visitDate"] = r.Visit!.VisitDate.ToString("yyyy-MM-dd"),
            ["visitNumber"] = r.Visit.VisitNumber,
            ["patientName"] = r.Visit.Patient!.FirstName + " " + r.Visit.Patient.LastName,
            ["referringDoctor"] = r.Visit.Doctor != null ? r.Visit.Doctor.User!.FullName : "—",
            ["direction"] = r.Direction.ToString(),
            ["referredTo"] = r.ReferredToDoctor != null ? r.ReferredToDoctor.User!.FullName : r.ExternalDetails,
            ["reason"] = r.Reason,
            ["opinionStatus"] = r.Direction == ReferralDirection.Internal ? (r.OpinionEnteredAt is null ? "Pending" : "Completed") : "-",
        }).ToList();
        return new ReportResultDto("Referral Report", columns, reportRows);
    }

    /// <summary>"OP to IP Conversion Report" — completed OPD visits vs. how many produced an
    /// admission via `Admission.ReferringOpVisitId` (the loose, existing conversion link — no
    /// new schema). Summary groups by date; Detail lists each completed visit and whether/where
    /// it converted.</summary>
    public async Task<ReportResultDto> GetConversionReportAsync(ConversionReportQuery query, CancellationToken ct = default)
    {
        var mode = (query.Mode ?? "summary").ToLowerInvariant();
        if (mode is not ("summary" or "detail"))
        {
            throw new DomainException($"Unknown report mode '{query.Mode}' — use 'summary' or 'detail'.");
        }

        var visits = db.OpVisits.AsNoTracking()
            .Where(v => v.Category == VisitCategory.Opd && v.Status == OpVisitStatus.Completed)
            .Include(v => v.Patient).ThenInclude(p => p!.PatientCategory)
            .Include(v => v.Doctor).ThenInclude(d => d!.User)
            .Include(v => v.Doctor).ThenInclude(d => d!.DoctorDepartments).ThenInclude(dd => dd.Department)
            .AsQueryable();
        if (query.FromDate is { } from) visits = visits.Where(v => v.VisitDate >= from);
        if (query.ToDate is { } to) visits = visits.Where(v => v.VisitDate <= to);
        if (query.DoctorId is { } doctorId) visits = visits.Where(v => v.DoctorId == doctorId);
        if (query.DepartmentId is { } departmentId) visits = visits.Where(v => v.Doctor!.DoctorDepartments.Any(dd => dd.DepartmentId == departmentId));
        if (query.PatientCategoryId is { } categoryId) visits = visits.Where(v => v.Patient!.PatientCategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(query.RegNo)) visits = visits.Where(v => v.Patient!.Uhid.Contains(query.RegNo));
        if (!string.IsNullOrWhiteSpace(query.PatientName))
        {
            var name = query.PatientName.Trim();
            visits = visits.Where(v => (v.Patient!.FirstName + " " + v.Patient.LastName).Contains(name));
        }

        var rows = await visits.OrderByDescending(v => v.VisitDate).ToListAsync(ct);
        var visitIds = rows.Select(v => v.Id).ToList();
        var admissionsByVisit = (await db.Admissions.AsNoTracking()
            .Where(a => a.ReferringOpVisitId != null && visitIds.Contains(a.ReferringOpVisitId!.Value))
            .ToListAsync(ct))
            .GroupBy(a => a.ReferringOpVisitId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        if (mode == "detail")
        {
            IReadOnlyList<ReportColumnDto> detailColumns =
            [
                new("visitDate", "Date"), new("patientUhid", "UHID"), new("patientName", "Patient"),
                new("patientType", "Patient Type"), new("doctorName", "Doctor"), new("department", "Department"),
                new("convertedToIp", "Converted to IP"), new("admissionNumber", "IP No"), new("admissionDate", "Admission Date"),
            ];
            var detailRows = rows.Select(v =>
            {
                var admission = admissionsByVisit.GetValueOrDefault(v.Id);
                var deptNames = v.Doctor?.DoctorDepartments.Select(dd => dd.Department!.Name).Distinct().ToList() ?? [];
                return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
                {
                    ["visitDate"] = v.VisitDate.ToString("yyyy-MM-dd"),
                    ["patientUhid"] = v.Patient!.Uhid,
                    ["patientName"] = v.Patient.FirstName + " " + v.Patient.LastName,
                    ["patientType"] = v.Patient.PatientCategory?.Name ?? "-",
                    ["doctorName"] = v.Doctor?.User?.FullName ?? "-",
                    ["department"] = deptNames.Count > 0 ? string.Join(", ", deptNames) : "-",
                    ["convertedToIp"] = admission != null ? "Yes" : "No",
                    ["admissionNumber"] = admission?.AdmissionNumber ?? "-",
                    ["admissionDate"] = admission?.AdmissionDate.ToString("yyyy-MM-dd") ?? "-",
                };
            }).ToList();
            return new ReportResultDto("OP to IP Conversion Report (Detail)", detailColumns, detailRows);
        }

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("visitDate", "Date"), new("completedVisits", "Completed Visits"),
            new("convertedToIp", "Converted to IP"), new("conversionRatePercent", "Conversion %"),
        ];
        var reportRows = rows
            .GroupBy(v => v.VisitDate)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var completed = g.Count();
                var converted = g.Count(v => admissionsByVisit.ContainsKey(v.Id));
                return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
                {
                    ["visitDate"] = g.Key.ToString("yyyy-MM-dd"),
                    ["completedVisits"] = completed,
                    ["convertedToIp"] = converted,
                    ["conversionRatePercent"] = completed == 0 ? 0 : Math.Round(converted * 100.0 / completed, 1),
                };
            }).ToList();
        return new ReportResultDto("OP to IP Conversion Report", columns, reportRows);
    }

    /// <summary>"MHC Package-wise Report" — MHC registrations grouped by package, over a date range.</summary>
    public async Task<ReportResultDto> GetMhcPackageWiseReportAsync(MhcReportQuery query, CancellationToken ct = default)
    {
        var rows = await MhcVisitsAsync(query, v => v.Include(x => x.MhcPackage), ct);

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("packageName", "Package"), new("newCount", "New"), new("reviewCount", "Review"),
            new("totalCount", "Total"), new("totalRevenue", "Total Revenue"),
        ];
        var reportRows = rows
            .GroupBy(v => v.MhcPackage != null ? v.MhcPackage.Name : "—")
            .OrderBy(g => g.Key)
            .Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["packageName"] = g.Key,
                ["newCount"] = g.Count(v => v.VisitType == VisitType.New),
                ["reviewCount"] = g.Count(v => v.VisitType == VisitType.Review),
                ["totalCount"] = g.Count(),
                ["totalRevenue"] = g.Sum(v => v.ConsultationFee),
            }).ToList();
        return new ReportResultDto("MHC Package-wise Report", columns, reportRows);
    }

    /// <summary>"MHC Doctor-wise Report" — MHC registrations grouped by panel doctor (MhcDoctor,
    /// not the regular Doctor roster), same New/Review shape as the OP Report summary.</summary>
    public async Task<ReportResultDto> GetMhcDoctorWiseReportAsync(MhcReportQuery query, CancellationToken ct = default)
    {
        var rows = await MhcVisitsAsync(query, v => v.Include(x => x.MhcDoctor), ct);

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("mhcDoctorName", "Panel Doctor"), new("newCount", "New"), new("reviewCount", "Review"),
            new("totalCount", "Total"), new("totalRevenue", "Total Revenue"),
        ];
        var reportRows = rows
            .GroupBy(v => v.MhcDoctor != null ? v.MhcDoctor.Name : "—")
            .OrderBy(g => g.Key)
            .Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["mhcDoctorName"] = g.Key,
                ["newCount"] = g.Count(v => v.VisitType == VisitType.New),
                ["reviewCount"] = g.Count(v => v.VisitType == VisitType.Review),
                ["totalCount"] = g.Count(),
                ["totalRevenue"] = g.Sum(v => v.ConsultationFee),
            }).ToList();
        return new ReportResultDto("MHC Doctor-wise Report", columns, reportRows);
    }

    /// <summary>"MHC Referrer-wise Report" — MHC volume/revenue by the patient's referral source
    /// (`Patient.ReferrerId`, a global patient attribute — not an MHC-specific link).</summary>
    public async Task<ReportResultDto> GetMhcReferrerWiseReportAsync(MhcReportQuery query, CancellationToken ct = default)
    {
        var rows = await MhcVisitsAsync(query, v => v.Include(x => x.Patient).ThenInclude(p => p!.Referrer), ct);

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("referrerName", "Referrer"), new("referrerType", "Type"), new("totalCount", "Total"),
            new("totalRevenue", "Total Revenue"),
        ];
        var reportRows = rows
            .GroupBy(v => v.Patient!.Referrer)
            .OrderBy(g => g.Key?.Name ?? "")
            .Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["referrerName"] = g.Key?.Name ?? "Walk-in / Direct",
                ["referrerType"] = g.Key?.Type.ToString() ?? "—",
                ["totalCount"] = g.Count(),
                ["totalRevenue"] = g.Sum(v => v.ConsultationFee),
            }).ToList();
        return new ReportResultDto("MHC Referrer-wise Report", columns, reportRows);
    }

    /// <summary>"Patient List Report - MHC" — flat listing of individual MHC registrations,
    /// distinct from the three aggregate breakdowns above.</summary>
    public async Task<ReportResultDto> GetMhcPatientListReportAsync(MhcReportQuery query, CancellationToken ct = default)
    {
        var rows = await MhcVisitsAsync(query, v => v
            .Include(x => x.Patient).ThenInclude(p => p!.PatientCategory)
            .Include(x => x.Patient).ThenInclude(p => p!.Referrer)
            .Include(x => x.MhcPackage)
            .Include(x => x.MhcDoctor), ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("visitDate", "Date"), new("patientUhid", "UHID"), new("patientName", "Patient"),
            new("age", "Age"), new("gender", "Gender"), new("patientType", "Patient Type"),
            new("package", "Package"), new("panelDoctor", "Panel Doctor"), new("visitType", "Visit Type"),
            new("referrer", "Referrer"), new("fee", "Fee"),
        ];
        var reportRows = rows.OrderByDescending(v => v.VisitDate).Select(v => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["visitDate"] = v.VisitDate.ToString("yyyy-MM-dd"),
            ["patientUhid"] = v.Patient!.Uhid,
            ["patientName"] = v.Patient.FirstName + " " + v.Patient.LastName,
            ["age"] = IpdMath.FormatAge(v.Patient.DateOfBirth, v.Patient.EstimatedAge, today),
            ["gender"] = v.Patient.Gender.ToString(),
            ["patientType"] = v.Patient.PatientCategory?.Name ?? "-",
            ["package"] = v.MhcPackage?.Name ?? "-",
            ["panelDoctor"] = v.MhcDoctor?.Name ?? "-",
            ["visitType"] = v.VisitType.ToString(),
            ["referrer"] = v.Patient.Referrer?.Name ?? "Walk-in / Direct",
            ["fee"] = v.ConsultationFee,
        }).ToList();
        return new ReportResultDto("Patient List Report - MHC", columns, reportRows);
    }

    /// <summary>"Service List Report - MHC" — every billed Service-sourced BillItem on an MHC
    /// visit's bill. Requires a Service pick (the reference tool marks this filter required) since
    /// it's meant to answer "who was billed for this specific service", not list every service.</summary>
    public async Task<ReportResultDto> GetMhcServiceListReportAsync(MhcServiceListReportQuery query, CancellationToken ct = default)
    {
        if (query.ServiceId is not { } serviceId)
        {
            throw new DomainException("Select a Service to run the Service List Report.");
        }

        var items = db.BillItems.AsNoTracking()
            .Include(i => i.Bill).ThenInclude(b => b!.Patient)
            .Include(i => i.Bill).ThenInclude(b => b!.OpVisit)
            .Include(i => i.Service)
            .Where(i => i.SourceType == BillItemSourceType.Service && i.ServiceId == serviceId
                && i.Bill!.OpVisit != null && i.Bill.OpVisit.Category == VisitCategory.Mhc)
            .AsQueryable();
        if (query.FromDate is { } from) items = items.Where(i => i.Bill!.BillDate >= from);
        if (query.ToDate is { } to) items = items.Where(i => i.Bill!.BillDate <= to);
        if (query.BillType is { } billType) items = items.Where(i => i.Bill!.BillType == billType);
        if (!string.IsNullOrWhiteSpace(query.RegNo)) items = items.Where(i => i.Bill!.Patient!.Uhid.Contains(query.RegNo));

        var rows = await items.OrderByDescending(i => i.Bill!.BillDate).ToListAsync(ct);

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("billDate", "Date"), new("billNumber", "Bill No"), new("billType", "Bill Type"),
            new("patientUhid", "UHID"), new("patientName", "Patient"), new("serviceName", "Service"),
            new("quantity", "Qty"), new("rate", "Rate"), new("totalAmount", "Amount"),
        ];
        var reportRows = rows.Select(i => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["billDate"] = i.Bill!.BillDate.ToString("yyyy-MM-dd"),
            ["billNumber"] = i.Bill.BillNumber,
            ["billType"] = i.Bill.BillType.ToString(),
            ["patientUhid"] = i.Bill.Patient!.Uhid,
            ["patientName"] = i.Bill.Patient.FirstName + " " + i.Bill.Patient.LastName,
            ["serviceName"] = i.Service?.Name ?? i.Description,
            ["quantity"] = i.Quantity,
            ["rate"] = i.Rate,
            ["totalAmount"] = i.TotalAmount,
        }).ToList();
        return new ReportResultDto("Service List Report - MHC", columns, reportRows);
    }

    private async Task<List<Domain.Entities.Opd.OpVisit>> MhcVisitsAsync(
        MhcReportQuery query,
        Func<IQueryable<Domain.Entities.Opd.OpVisit>, IQueryable<Domain.Entities.Opd.OpVisit>> include,
        CancellationToken ct)
    {
        var visits = include(db.OpVisits.AsNoTracking().Where(v => v.Category == VisitCategory.Mhc));
        if (query.FromDate is { } from) visits = visits.Where(v => v.VisitDate >= from);
        if (query.ToDate is { } to) visits = visits.Where(v => v.VisitDate <= to);
        if (query.MhcDoctorId is { } mhcDoctorId) visits = visits.Where(v => v.MhcDoctorId == mhcDoctorId);
        if (query.VisitType is { } visitType) visits = visits.Where(v => v.VisitType == visitType);
        if (query.PatientCategoryId is { } categoryId) visits = visits.Where(v => v.Patient!.PatientCategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(query.RegNo)) visits = visits.Where(v => v.Patient!.Uhid.Contains(query.RegNo));
        if (!string.IsNullOrWhiteSpace(query.PatientName))
        {
            var name = query.PatientName.Trim();
            visits = visits.Where(v => (v.Patient!.FirstName + " " + v.Patient.LastName).Contains(name));
        }
        return await visits.ToListAsync(ct);
    }
}
