using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Application.Features.Ipd;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Reports;

/// <summary>
/// OPIP Census Report — a front-office census blending OpVisit (Category == Opd) and Admission
/// counts, department-wise, split New/Review × Male/Female/Child. Distinct from the day-granular
/// mv_op_census/mv_daily_collection materialized-view dashboards (Phase 23) — this is a plain
/// live query over the two visit tables, matching the reference tool's own on-demand report.
///
/// Two department-membership and one New/Review caveat, both deliberate simplifications:
///  - A doctor can belong to more than one Department (DoctorDepartment is many-to-many); each
///    visit/admission is counted under only its doctor's *first* department (by Id) rather than
///    fanned out across all of them, same single-owner-per-row posture as a physical bed/ward.
///  - Admission has no New/Review concept in this schema (unlike OpVisit.VisitType) — every
///    admission in the date range is counted as "New" on the IP side; Review columns are always
///    0 for IP-sourced rows. Flag this to the user if their census defines IP "Review" differently
///    (e.g. patients still admitted from before the range) — that would need a different query.
/// </summary>
public class CensusReportService(IAppDbContext db, SettingService settings, TimeProvider timeProvider)
{
    private sealed class CensusBucket
    {
        public int NewFChild, NewMChild, NewMale, NewFemale, NewOther;
        public int ReviewFChild, ReviewMChild, ReviewMale, ReviewFemale, ReviewOther;
    }

    public async Task<ReportResultDto> GetDepartmentWiseCensusAsync(CensusReportQuery query, CancellationToken ct = default)
    {
        var childThreshold = await settings.GetIntAsync(SettingKeys.CensusChildAgeThresholdYears, 12, ct);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var buckets = new Dictionary<string, CensusBucket>();

        if (query.Scope != CensusScope.Ip)
        {
            var opVisits = db.OpVisits.AsNoTracking().Where(v => v.Category == VisitCategory.Opd)
                .Include(v => v.Patient)
                .Include(v => v.Doctor).ThenInclude(d => d!.DoctorDepartments).ThenInclude(dd => dd.Department)
                .AsQueryable();
            if (query.FromDate is { } from) opVisits = opVisits.Where(v => v.VisitDate >= from);
            if (query.ToDate is { } to) opVisits = opVisits.Where(v => v.VisitDate <= to);
            if (query.DepartmentId is { } deptId) opVisits = opVisits.Where(v => v.Doctor!.DoctorDepartments.Any(dd => dd.DepartmentId == deptId));
            if (query.PatientCategoryId is { } catId) opVisits = opVisits.Where(v => v.Patient!.PatientCategoryId == catId);

            foreach (var v in await opVisits.ToListAsync(ct))
            {
                var deptName = v.Doctor?.DoctorDepartments.Select(dd => dd.Department!.Name).FirstOrDefault() ?? "Unassigned";
                var age = IpdMath.ComputeAgeYears(v.Patient!.DateOfBirth, v.Patient.EstimatedAge, today);
                Accumulate(GetBucket(buckets, deptName), v.VisitType == VisitType.New, age is { } a && a < childThreshold, v.Patient.Gender);
            }
        }

        if (query.Scope != CensusScope.Op)
        {
            var admissions = db.Admissions.AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.PrimaryConsultant).ThenInclude(d => d!.DoctorDepartments).ThenInclude(dd => dd.Department)
                .Include(a => a.CurrentBed).ThenInclude(b => b!.Room)
                .AsQueryable();
            if (query.FromDate is { } from) admissions = admissions.Where(a => a.AdmissionDate >= AtStart(from));
            if (query.ToDate is { } to) admissions = admissions.Where(a => a.AdmissionDate < AtStart(to.AddDays(1)));
            if (query.DepartmentId is { } deptId) admissions = admissions.Where(a => a.PrimaryConsultant!.DoctorDepartments.Any(dd => dd.DepartmentId == deptId));
            if (query.WardId is { } wardId) admissions = admissions.Where(a => a.CurrentBed!.Room!.WardId == wardId);
            if (query.PatientCategoryId is { } catId) admissions = admissions.Where(a => a.Patient!.PatientCategoryId == catId);

            foreach (var a in await admissions.ToListAsync(ct))
            {
                var deptName = a.PrimaryConsultant?.DoctorDepartments.Select(dd => dd.Department!.Name).FirstOrDefault() ?? "Unassigned";
                var age = IpdMath.ComputeAgeYears(a.Patient!.DateOfBirth, a.Patient.EstimatedAge, today);
                Accumulate(GetBucket(buckets, deptName), isNew: true, age is { } ay && ay < childThreshold, a.Patient.Gender);
            }
        }

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("department", "Department"),
            new("newFChild", "New F-Child"), new("newMChild", "New M-Child"),
            new("newMale", "New-Male"), new("newFemale", "New-Female"), new("newOther", "New-Other"), new("newTotal", "New-Total"),
            new("reviewFChild", "Review F-Child"), new("reviewMChild", "Review M-Child"),
            new("reviewMale", "Review-Male"), new("reviewFemale", "Review-Female"), new("reviewOther", "Review-Other"), new("reviewTotal", "Review-Total"),
            new("grandTotal", "Grand Total"),
        ];

        var rows = buckets.OrderBy(kv => kv.Key).Select(kv =>
        {
            var b = kv.Value;
            var newTotal = b.NewFChild + b.NewMChild + b.NewMale + b.NewFemale + b.NewOther;
            var reviewTotal = b.ReviewFChild + b.ReviewMChild + b.ReviewMale + b.ReviewFemale + b.ReviewOther;
            return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["department"] = kv.Key,
                ["newFChild"] = b.NewFChild, ["newMChild"] = b.NewMChild,
                ["newMale"] = b.NewMale, ["newFemale"] = b.NewFemale, ["newOther"] = b.NewOther, ["newTotal"] = newTotal,
                ["reviewFChild"] = b.ReviewFChild, ["reviewMChild"] = b.ReviewMChild,
                ["reviewMale"] = b.ReviewMale, ["reviewFemale"] = b.ReviewFemale, ["reviewOther"] = b.ReviewOther, ["reviewTotal"] = reviewTotal,
                ["grandTotal"] = newTotal + reviewTotal,
            };
        }).ToList();

        var reportName = query.Scope switch
        {
            CensusScope.Op => "OP Census Department Wise",
            CensusScope.Ip => "IP Census Department Wise",
            _ => "OPIP Census Department Wise",
        };
        return new ReportResultDto(reportName, columns, rows);
    }

    /// <summary>"Patient Wise Details" — the flat listing of individual visits/admissions behind
    /// the department-wise census, same filters and Child/Male/Female bucketing.</summary>
    public async Task<ReportResultDto> GetPatientWiseDetailsAsync(CensusReportQuery query, CancellationToken ct = default)
    {
        var childThreshold = await settings.GetIntAsync(SettingKeys.CensusChildAgeThresholdYears, 12, ct);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var rows = new List<IReadOnlyDictionary<string, object?>>();

        if (query.Scope != CensusScope.Ip)
        {
            var opVisits = db.OpVisits.AsNoTracking().Where(v => v.Category == VisitCategory.Opd)
                .Include(v => v.Patient)
                .Include(v => v.Doctor).ThenInclude(d => d!.User)
                .Include(v => v.Doctor).ThenInclude(d => d!.DoctorDepartments).ThenInclude(dd => dd.Department)
                .AsQueryable();
            if (query.FromDate is { } from) opVisits = opVisits.Where(v => v.VisitDate >= from);
            if (query.ToDate is { } to) opVisits = opVisits.Where(v => v.VisitDate <= to);
            if (query.DepartmentId is { } deptId) opVisits = opVisits.Where(v => v.Doctor!.DoctorDepartments.Any(dd => dd.DepartmentId == deptId));
            if (query.PatientCategoryId is { } catId) opVisits = opVisits.Where(v => v.Patient!.PatientCategoryId == catId);

            rows.AddRange((await opVisits.OrderBy(v => v.VisitDate).ToListAsync(ct)).Select(v =>
            {
                var age = IpdMath.ComputeAgeYears(v.Patient!.DateOfBirth, v.Patient.EstimatedAge, today);
                var isChild = age is { } a && a < childThreshold;
                return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
                {
                    ["source"] = "OP",
                    ["regNo"] = v.Patient!.Uhid,
                    ["patientName"] = v.Patient.FirstName + " " + v.Patient.LastName,
                    ["age"] = IpdMath.FormatAge(v.Patient.DateOfBirth, v.Patient.EstimatedAge, today),
                    ["gender"] = v.Patient.Gender.ToString(),
                    ["bucket"] = isChild ? "Child" : v.Patient.Gender.ToString(),
                    ["visitType"] = v.VisitType.ToString(),
                    ["department"] = v.Doctor?.DoctorDepartments.Select(dd => dd.Department!.Name).FirstOrDefault() ?? "Unassigned",
                    ["doctor"] = v.Doctor?.User?.FullName ?? "-",
                    ["ward"] = "-",
                    ["date"] = v.VisitDate.ToString("yyyy-MM-dd"),
                };
            }));
        }

        if (query.Scope != CensusScope.Op)
        {
            var admissions = db.Admissions.AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.PrimaryConsultant).ThenInclude(d => d!.User)
                .Include(a => a.PrimaryConsultant).ThenInclude(d => d!.DoctorDepartments).ThenInclude(dd => dd.Department)
                .Include(a => a.CurrentBed).ThenInclude(b => b!.Room).ThenInclude(r => r!.Ward)
                .AsQueryable();
            if (query.FromDate is { } from) admissions = admissions.Where(a => a.AdmissionDate >= AtStart(from));
            if (query.ToDate is { } to) admissions = admissions.Where(a => a.AdmissionDate < AtStart(to.AddDays(1)));
            if (query.DepartmentId is { } deptId) admissions = admissions.Where(a => a.PrimaryConsultant!.DoctorDepartments.Any(dd => dd.DepartmentId == deptId));
            if (query.WardId is { } wardId) admissions = admissions.Where(a => a.CurrentBed!.Room!.WardId == wardId);
            if (query.PatientCategoryId is { } catId) admissions = admissions.Where(a => a.Patient!.PatientCategoryId == catId);

            rows.AddRange((await admissions.OrderBy(a => a.AdmissionDate).ToListAsync(ct)).Select(a =>
            {
                var age = IpdMath.ComputeAgeYears(a.Patient!.DateOfBirth, a.Patient.EstimatedAge, today);
                var isChild = age is { } ay && ay < childThreshold;
                return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
                {
                    ["source"] = "IP",
                    ["regNo"] = a.Patient!.Uhid,
                    ["patientName"] = a.Patient.FirstName + " " + a.Patient.LastName,
                    ["age"] = IpdMath.FormatAge(a.Patient.DateOfBirth, a.Patient.EstimatedAge, today),
                    ["gender"] = a.Patient.Gender.ToString(),
                    ["bucket"] = isChild ? "Child" : a.Patient.Gender.ToString(),
                    ["visitType"] = "New", // Admission has no Review concept — see class doc.
                    ["department"] = a.PrimaryConsultant?.DoctorDepartments.Select(dd => dd.Department!.Name).FirstOrDefault() ?? "Unassigned",
                    ["doctor"] = a.PrimaryConsultant?.User?.FullName ?? "-",
                    ["ward"] = a.CurrentBed?.Room?.Ward?.Name ?? "-",
                    ["date"] = a.AdmissionDate.ToString("yyyy-MM-dd"),
                };
            }));
        }

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("source", "Source"), new("regNo", "UHID"), new("patientName", "Patient"),
            new("age", "Age"), new("gender", "Gender"), new("bucket", "Bucket"), new("visitType", "Visit Type"),
            new("department", "Department"), new("doctor", "Doctor"), new("ward", "Ward"), new("date", "Date"),
        ];
        return new ReportResultDto("OPIP Census — Patient Wise Details", columns, rows);
    }

    private static CensusBucket GetBucket(Dictionary<string, CensusBucket> buckets, string department)
    {
        if (!buckets.TryGetValue(department, out var bucket))
        {
            bucket = new CensusBucket();
            buckets[department] = bucket;
        }
        return bucket;
    }

    private static void Accumulate(CensusBucket bucket, bool isNew, bool isChild, Gender gender)
    {
        if (isChild)
        {
            if (gender == Gender.Female) { if (isNew) bucket.NewFChild++; else bucket.ReviewFChild++; }
            else if (gender == Gender.Male) { if (isNew) bucket.NewMChild++; else bucket.ReviewMChild++; }
            else { if (isNew) bucket.NewOther++; else bucket.ReviewOther++; }
        }
        else
        {
            if (gender == Gender.Female) { if (isNew) bucket.NewFemale++; else bucket.ReviewFemale++; }
            else if (gender == Gender.Male) { if (isNew) bucket.NewMale++; else bucket.ReviewMale++; }
            else { if (isNew) bucket.NewOther++; else bucket.ReviewOther++; }
        }
    }

    private static DateTimeOffset AtStart(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
