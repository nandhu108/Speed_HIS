using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Reports;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Ipd;

/// <summary>
/// IP Report (admissions) and Ward Occupancy Report — standalone, date-range-filterable IPD
/// reports with a Summary/Detail toggle and CSV/Excel/PDF export (via the shared
/// <see cref="ReportExportHelper"/>). Global EF query filters already scope by branch — no
/// manual branch filtering needed here, matching MrdReportService/BiomedReportService.
/// </summary>
public class IpReportService(IAppDbContext db, TimeProvider timeProvider)
{
    public async Task<ReportResultDto> GetAdmissionsReportAsync(IpReportQuery query, CancellationToken ct = default)
    {
        var mode = ValidateMode(query.Mode);

        var admissions = db.Admissions.AsNoTracking()
            .Include(a => a.Patient).ThenInclude(p => p!.PatientCategory)
            .Include(a => a.Patient).ThenInclude(p => p!.Referrer)
            .Include(a => a.PrimaryConsultant).ThenInclude(d => d!.User)
            .Include(a => a.PrimaryConsultant).ThenInclude(d => d!.DoctorDepartments).ThenInclude(dd => dd.Department)
            .Include(a => a.CurrentBed).ThenInclude(b => b!.Room).ThenInclude(r => r!.Ward).ThenInclude(w => w!.Floor)
            .Include(a => a.CurrentBed).ThenInclude(b => b!.Room).ThenInclude(r => r!.RoomType)
            .Include(a => a.ReferringOpVisit).ThenInclude(v => v!.Camp)
            .AsQueryable();
        if (query.FromDate is { } from) admissions = admissions.Where(a => a.AdmissionDate >= AtStart(from));
        if (query.ToDate is { } to) admissions = admissions.Where(a => a.AdmissionDate < AtStart(to.AddDays(1)));
        if (query.DoctorId is { } doctorId) admissions = admissions.Where(a => a.PrimaryConsultantId == doctorId);
        if (query.WardId is { } wardId) admissions = admissions.Where(a => a.CurrentBed!.Room!.WardId == wardId);
        if (query.DepartmentId is { } departmentId) admissions = admissions.Where(a => a.PrimaryConsultant!.DoctorDepartments.Any(dd => dd.DepartmentId == departmentId));
        if (query.PatientCategoryId is { } categoryId) admissions = admissions.Where(a => a.Patient!.PatientCategoryId == categoryId);
        if (query.ReferrerId is { } referrerId) admissions = admissions.Where(a => a.Patient!.ReferrerId == referrerId);
        if (query.RoomTypeId is { } roomTypeId) admissions = admissions.Where(a => a.CurrentBed!.Room!.RoomTypeId == roomTypeId);
        if (query.Status is { } status) admissions = admissions.Where(a => a.Status == status);
        if (!string.IsNullOrWhiteSpace(query.RegNo)) admissions = admissions.Where(a => a.Patient!.Uhid.Contains(query.RegNo));
        if (!string.IsNullOrWhiteSpace(query.IpNo)) admissions = admissions.Where(a => a.AdmissionNumber.Contains(query.IpNo));
        if (!string.IsNullOrWhiteSpace(query.PatientName))
        {
            var name = query.PatientName.Trim();
            admissions = admissions.Where(a => (a.Patient!.FirstName + " " + a.Patient.LastName).Contains(name));
        }
        if (query.UnitId is { } unitId)
        {
            var doctorIdsInUnit = await db.UnitDoctors.AsNoTracking()
                .Where(ud => ud.UnitId == unitId)
                .Select(ud => ud.DoctorId)
                .ToListAsync(ct);
            admissions = admissions.Where(a => doctorIdsInUnit.Contains(a.PrimaryConsultantId));
        }

        var rows = await admissions.OrderBy(a => a.AdmissionDate).ToListAsync(ct);
        var now = timeProvider.GetUtcNow();

        if (mode == "detail")
        {
            return await BuildDetailReportAsync(rows, now, ct);
        }

        IReadOnlyList<ReportColumnDto> summaryColumns =
        [
            new("wardName", "Ward"), new("admissionsCount", "Admissions"),
            new("dischargesCount", "Discharges"), new("avgLosDays", "Avg LOS (days)"),
        ];

        // Discharges are filtered by DischargedAt, admissions above by AdmissionDate — an admission
        // discharged in-range may have been admitted outside it, so this is queried completely
        // independently of `rows` (same double-filter posture as
        // DashboardService.GetIpDashboardAsync's admissionsInRange/discharged split).
        var dischargedInRangeQuery = db.Admissions.AsNoTracking()
            .Include(a => a.CurrentBed).ThenInclude(b => b!.Room).ThenInclude(r => r!.Ward)
            .Where(a => a.Status == AdmissionStatus.Discharged && a.DischargedAt != null);
        if (query.FromDate is { } dFrom) dischargedInRangeQuery = dischargedInRangeQuery.Where(a => a.DischargedAt >= AtStart(dFrom));
        if (query.ToDate is { } dTo) dischargedInRangeQuery = dischargedInRangeQuery.Where(a => a.DischargedAt < AtStart(dTo.AddDays(1)));
        var allDischarged = await dischargedInRangeQuery.ToListAsync(ct);

        var wardNames = rows.Select(a => a.CurrentBed!.Room!.Ward!.Name)
            .Concat(allDischarged.Select(a => a.CurrentBed!.Room!.Ward!.Name))
            .Distinct().OrderBy(n => n);

        var summaryRows = wardNames.Select(wardName =>
        {
            var wardAdmissions = rows.Count(a => a.CurrentBed!.Room!.Ward!.Name == wardName);
            var wardDischarges = allDischarged.Where(a => a.CurrentBed!.Room!.Ward!.Name == wardName).ToList();
            decimal? avgLos = wardDischarges.Count == 0 ? null :
                Math.Round((decimal)wardDischarges.Average(a => (a.DischargedAt!.Value - a.AdmissionDate).TotalDays), 1);
            return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["wardName"] = wardName,
                ["admissionsCount"] = wardAdmissions,
                ["dischargesCount"] = wardDischarges.Count,
                ["avgLosDays"] = avgLos,
            };
        }).ToList();
        return new ReportResultDto("IP Report (Summary)", summaryColumns, summaryRows);
    }

    /// <summary>
    /// Builds the IP Report detail rows plus everything resolvable from existing schema: ABHA ID,
    /// the active secondary consultant(s), department/unit (both resolved through the primary
    /// consultant, same as OpReportService), the primary guardian/attender contact, whether the
    /// admission came from an OP visit or an ER visit, the visit's bill status, and who created
    /// it. Each is bulk-loaded by id set rather than per-row. Deliberately does not include
    /// Corporate/Corporate Type/Religion/Area/Case Type/MLC/Coordinator/Case Manager or a
    /// referral-doctor's city/status — none of those exist anywhere in the schema yet, same
    /// scoping call as the OP Report.
    /// </summary>
    private async Task<ReportResultDto> BuildDetailReportAsync(List<Domain.Entities.Ipd.Admission> rows, DateTimeOffset now, CancellationToken ct)
    {
        var admissionIds = rows.Select(a => a.Id).ToList();
        var patientIds = rows.Select(a => a.PatientId).Distinct().ToList();
        var doctorIds = rows.Select(a => a.PrimaryConsultantId).Distinct().ToList();
        var creatorIds = rows.Select(a => a.CreatedBy).Distinct().ToList();

        var abhaByPatient = (await db.PatientIdentifiers.AsNoTracking()
            .Where(i => patientIds.Contains(i.PatientId) && i.Type == PatientIdentifierType.Abha)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct))
            .GroupBy(i => i.PatientId)
            .ToDictionary(g => g.Key, g => g.First().Value);

        var primaryContactByPatient = (await db.PatientContacts.AsNoTracking()
            .Where(c => patientIds.Contains(c.PatientId))
            .OrderByDescending(c => c.IsPrimary).ThenBy(c => c.Id)
            .ToListAsync(ct))
            .GroupBy(c => c.PatientId)
            .ToDictionary(g => g.Key, g => g.First());

        var secondaryDoctorsByAdmission = (await db.AdmissionConsultants.AsNoTracking()
            .Where(ac => admissionIds.Contains(ac.AdmissionId) && ac.Role == ConsultantRole.Secondary && ac.EndedAt == null)
            .Include(ac => ac.Doctor).ThenInclude(d => d!.User)
            .ToListAsync(ct))
            .GroupBy(ac => ac.AdmissionId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(ac => ac.Doctor!.User!.FullName).Distinct()));

        var unitNamesByDoctor = (await db.UnitDoctors.AsNoTracking()
            .Where(ud => doctorIds.Contains(ud.DoctorId))
            .Include(ud => ud.Unit)
            .ToListAsync(ct))
            .GroupBy(ud => ud.DoctorId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(ud => ud.Unit!.Name).Distinct()));

        var billStatusByAdmission = (await db.Bills.AsNoTracking()
            .Where(b => b.AdmissionId != null && admissionIds.Contains(b.AdmissionId!.Value))
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct))
            .GroupBy(b => b.AdmissionId!.Value)
            .ToDictionary(g => g.Key, g => g.First().Status.ToString());

        var userNameById = await db.Users.AsNoTracking()
            .Where(u => creatorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var today = DateOnly.FromDateTime(now.UtcDateTime);

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("admissionNumber", "IP No"), new("patientUhid", "UHID"), new("abhaId", "ABHA ID"),
            new("admissionDate", "Admission Date"), new("opToIp", "OP to IP"), new("erToIpConvert", "ER to IP Convert"),
            new("patientName", "Patient"), new("patientType", "Patient Type"), new("age", "Age"), new("gender", "Gender"),
            new("relation", "Relation"), new("relationName", "Relation Name"), new("occupation", "Occupation"),
            new("mobile", "Mobile"), new("address", "Address"), new("email", "Email"),
            new("department", "Department"), new("doctorName", "Doctor"), new("secondaryDoctor", "Secondary Doctor"),
            new("referredSource", "Referred Source"), new("referredSourceName", "Referred Source Name"), new("camp", "Camp"),
            new("floor", "Floor"), new("wardName", "Ward"), new("bedCode", "Bed No"), new("bedType", "Bed Type"),
            new("unit", "Unit"), new("status", "Status"), new("dischargedAt", "Discharge Date"), new("duration", "Duration"),
            new("ipDiagnosis", "IP Diagnosis"), new("nationality", "Nationality"), new("billStatus", "Bill Status"),
            new("createdUser", "Created User"),
        ];

        var detailRows = rows.Select(a =>
        {
            var contact = primaryContactByPatient.GetValueOrDefault(a.PatientId);
            var duration = (a.DischargedAt ?? now) - a.AdmissionDate;
            var addressParts = new[] { a.Patient!.Address, a.Patient.City?.Name }.Where(s => !string.IsNullOrWhiteSpace(s));

            return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["admissionNumber"] = a.AdmissionNumber,
                ["patientUhid"] = a.Patient.Uhid,
                ["abhaId"] = abhaByPatient.GetValueOrDefault(a.PatientId, "-"),
                ["admissionDate"] = a.AdmissionDate.ToString("yyyy-MM-dd HH:mm"),
                ["opToIp"] = a.ReferringOpVisitId != null ? "Yes" : "No",
                ["erToIpConvert"] = a.ReferringEmergencyVisitId != null ? "Yes" : "No",
                ["patientName"] = a.Patient.FirstName + " " + a.Patient.LastName,
                ["patientType"] = a.Patient.PatientCategory?.Name ?? "-",
                ["age"] = IpdMath.FormatAge(a.Patient.DateOfBirth, a.Patient.EstimatedAge, today),
                ["gender"] = a.Patient.Gender.ToString(),
                ["relation"] = contact?.Relationship ?? "-",
                ["relationName"] = contact?.Name ?? "-",
                ["occupation"] = a.Patient.Occupation ?? "-",
                ["mobile"] = a.Patient.Mobile,
                ["address"] = string.Join(", ", addressParts),
                ["email"] = a.Patient.Email ?? "-",
                ["department"] = a.PrimaryConsultant?.DoctorDepartments.Select(dd => dd.Department!.Name).Distinct().ToList() is { Count: > 0 } depts
                    ? string.Join(", ", depts) : "-",
                ["doctorName"] = a.PrimaryConsultant?.User?.FullName ?? "-",
                ["secondaryDoctor"] = secondaryDoctorsByAdmission.GetValueOrDefault(a.Id, "-"),
                ["referredSource"] = a.Patient.Referrer?.Type.ToString() ?? "-",
                ["referredSourceName"] = a.Patient.Referrer?.Name ?? "-",
                ["camp"] = a.ReferringOpVisit?.Camp?.Name ?? "-",
                ["floor"] = a.CurrentBed?.Room?.Ward?.Floor?.Name ?? "-",
                ["wardName"] = a.CurrentBed?.Room?.Ward?.Name ?? "-",
                ["bedCode"] = a.CurrentBed?.Code ?? "-",
                ["bedType"] = a.CurrentBed?.Room?.RoomType?.Name ?? "-",
                ["unit"] = unitNamesByDoctor.GetValueOrDefault(a.PrimaryConsultantId, "-"),
                ["status"] = a.Status.ToString(),
                ["dischargedAt"] = a.DischargedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-",
                ["duration"] = FormatDuration(duration),
                ["ipDiagnosis"] = a.ProvisionalDiagnosis ?? "-",
                ["nationality"] = a.Patient.Nationality?.Name,
                ["billStatus"] = billStatusByAdmission.GetValueOrDefault(a.Id, "No Bill"),
                ["createdUser"] = userNameById.GetValueOrDefault(a.CreatedBy, "-"),
            };
        }).ToList();
        return new ReportResultDto("IP Report (Detail)", columns, detailRows);
    }

    private static string FormatDuration(TimeSpan span)
    {
        var totalMinutes = Math.Max(0, (int)Math.Round(span.TotalMinutes));
        var days = totalMinutes / (24 * 60);
        var hours = totalMinutes % (24 * 60) / 60;
        var minutes = totalMinutes % 60;
        return $"{days}D {hours}H {minutes}M";
    }

    public async Task<ReportResultDto> GetWardOccupancyReportAsync(WardOccupancyReportQuery query, CancellationToken ct = default)
    {
        var mode = ValidateMode(query.Mode);
        if (query.FromDate is not { } fromDate || query.ToDate is not { } toDate)
        {
            throw new DomainException("Select both a from-date and a to-date for the Ward Occupancy Report.");
        }
        var rangeStart = AtStart(fromDate);
        var rangeEnd = AtStart(toDate.AddDays(1));
        var totalRangeDays = (decimal)(rangeEnd - rangeStart).TotalDays;

        List<long>? doctorIdsInUnit = null;
        if (query.UnitId is { } unitId)
        {
            doctorIdsInUnit = await db.UnitDoctors.AsNoTracking()
                .Where(ud => ud.UnitId == unitId)
                .Select(ud => ud.DoctorId)
                .ToListAsync(ct);
        }

        var beds = db.Beds.AsNoTracking()
            .Include(b => b.Room).ThenInclude(r => r!.Ward)
            .AsQueryable();
        if (query.WardId is { } wardId) beds = beds.Where(b => b.Room!.WardId == wardId);
        if (query.BedStatus is { } bedStatus) beds = beds.Where(b => b.Status == bedStatus);
        if (query.WardFlag == "icu") beds = beds.Where(b => b.Room!.Ward!.IsIcu);
        else if (query.WardFlag == "observation") beds = beds.Where(b => b.Room!.Ward!.IsObservation);
        var bedIds = (await beds.Select(b => b.Id).ToListAsync(ct)).ToHashSet();

        var allocations = await db.BedAllocations.AsNoTracking()
            .Include(ba => ba.Bed).ThenInclude(b => b!.Room).ThenInclude(r => r!.Ward)
            .Include(ba => ba.Admission).ThenInclude(a => a!.Patient)
            .Where(ba => ba.AllocatedAt < rangeEnd && (ba.ReleasedAt == null || ba.ReleasedAt > rangeStart) && bedIds.Contains(ba.BedId))
            .ToListAsync(ct);
        if (query.DoctorId is { } doctorId) allocations = allocations.Where(ba => ba.Admission!.PrimaryConsultantId == doctorId).ToList();
        if (doctorIdsInUnit != null) allocations = allocations.Where(ba => doctorIdsInUnit.Contains(ba.Admission!.PrimaryConsultantId)).ToList();

        var occupied = allocations.Select(ba => new
        {
            Allocation = ba,
            DaysInRange = OccupancyMath.OverlapDays(ba.AllocatedAt, ba.ReleasedAt, rangeStart, rangeEnd),
        }).OrderBy(x => x.Allocation.Bed!.Room!.Ward!.Name).ThenBy(x => x.Allocation.AllocatedAt).ToList();

        if (mode == "detail")
        {
            IReadOnlyList<ReportColumnDto> detailColumns =
            [
                new("wardName", "Ward"), new("roomName", "Room"), new("bedCode", "Bed"),
                new("patientName", "Patient"), new("patientUhid", "UHID"), new("admissionNumber", "Admission No"),
                new("doctorName", "Doctor"), new("allocatedAt", "Allocated At"), new("releasedAt", "Released At"), new("daysInRange", "Days In Range"),
            ];
            var consultantIds = occupied.Select(x => x.Allocation.Admission!.PrimaryConsultantId).Distinct().ToList();
            var doctorNames = await db.Doctors.AsNoTracking().Include(d => d.User)
                .Where(d => consultantIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.User!.FullName, ct);
            var detailRows = occupied.Select(x => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["wardName"] = x.Allocation.Bed!.Room!.Ward!.Name,
                ["roomName"] = x.Allocation.Bed.Room.Name,
                ["bedCode"] = x.Allocation.Bed.Code,
                ["patientName"] = x.Allocation.Admission!.Patient!.FirstName + " " + x.Allocation.Admission.Patient.LastName,
                ["patientUhid"] = x.Allocation.Admission.Patient.Uhid,
                ["admissionNumber"] = x.Allocation.Admission.AdmissionNumber,
                ["doctorName"] = doctorNames.GetValueOrDefault(x.Allocation.Admission.PrimaryConsultantId, "-"),
                ["allocatedAt"] = x.Allocation.AllocatedAt.ToString("yyyy-MM-dd HH:mm"),
                ["releasedAt"] = x.Allocation.ReleasedAt?.ToString("yyyy-MM-dd HH:mm") ?? "Ongoing",
                ["daysInRange"] = Math.Round(x.DaysInRange, 1),
            }).ToList();
            return new ReportResultDto("Ward Occupancy Report (Detail)", detailColumns, detailRows);
        }

        var bedCountsByWard = await beds
            .GroupBy(b => b.Room!.Ward!.Name)
            .Select(g => new { WardName = g.Key, BedCount = g.Count() })
            .ToListAsync(ct);

        IReadOnlyList<ReportColumnDto> summaryColumns =
        [
            new("wardName", "Ward"), new("bedCount", "Beds"), new("bedDaysAvailable", "Bed-Days Available"),
            new("bedDaysOccupied", "Bed-Days Occupied"), new("occupancyPercent", "Occupancy %"),
        ];
        var summaryRows = bedCountsByWard.OrderBy(w => w.WardName).Select(w =>
        {
            var bedDaysOccupied = occupied.Where(x => x.Allocation.Bed!.Room!.Ward!.Name == w.WardName).Sum(x => x.DaysInRange);
            var bedDaysAvailable = w.BedCount * totalRangeDays;
            var occupancyPercent = bedDaysAvailable == 0 ? 0m : Math.Round(bedDaysOccupied * 100m / bedDaysAvailable, 1);
            return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["wardName"] = w.WardName,
                ["bedCount"] = w.BedCount,
                ["bedDaysAvailable"] = Math.Round(bedDaysAvailable, 1),
                ["bedDaysOccupied"] = Math.Round(bedDaysOccupied, 1),
                ["occupancyPercent"] = occupancyPercent,
            };
        }).ToList();
        return new ReportResultDto("Ward Occupancy Report (Summary)", summaryColumns, summaryRows);
    }

    /// <summary>"Doctor Admission Request" conversion report — every AdmissionRequest in range,
    /// whether it converted to a real Admission (ApprovedAdmissionId), and who's involved on
    /// each side of that conversion. Flat listing only (the reference tool's own screen has no
    /// Summary/Detail toggle here, unlike the admissions/occupancy reports above).</summary>
    public async Task<ReportResultDto> GetAdmissionRequestConversionReportAsync(
        AdmissionRequestConversionReportQuery query, CancellationToken ct = default)
    {
        var requests = db.AdmissionRequests.AsNoTracking()
            .Include(r => r.Patient).ThenInclude(p => p!.PatientCategory)
            .Include(r => r.RequestedConsultant).ThenInclude(d => d!.User)
            .Include(r => r.RequestedConsultant).ThenInclude(d => d!.DoctorDepartments).ThenInclude(dd => dd.Department)
            .Include(r => r.Ward)
            .Include(r => r.ApprovedAdmission).ThenInclude(a => a!.PrimaryConsultant).ThenInclude(d => d!.User)
            .Include(r => r.ApprovedAdmission).ThenInclude(a => a!.CurrentBed).ThenInclude(b => b!.Room).ThenInclude(rm => rm!.RoomType)
            .AsQueryable();
        if (query.FromDate is { } from) requests = requests.Where(r => r.ExpectedAdmissionDate >= from);
        if (query.ToDate is { } to) requests = requests.Where(r => r.ExpectedAdmissionDate <= to);
        if (query.Status is { } status) requests = requests.Where(r => r.Status == status);
        if (query.AdvisedDoctorId is { } advisedId) requests = requests.Where(r => r.RequestedConsultantId == advisedId);
        if (query.DoctorId is { } doctorId) requests = requests.Where(r => r.ApprovedAdmission!.PrimaryConsultantId == doctorId);
        if (query.DepartmentId is { } deptId) requests = requests.Where(r => r.RequestedConsultant!.DoctorDepartments.Any(dd => dd.DepartmentId == deptId));
        if (query.WardId is { } wardId) requests = requests.Where(r => r.WardId == wardId);
        if (query.RoomTypeId is { } roomTypeId) requests = requests.Where(r => r.ApprovedAdmission!.CurrentBed!.Room!.RoomTypeId == roomTypeId);
        if (query.PatientCategoryId is { } catId) requests = requests.Where(r => r.Patient!.PatientCategoryId == catId);
        if (!string.IsNullOrWhiteSpace(query.RegNo)) requests = requests.Where(r => r.Patient!.Uhid.Contains(query.RegNo));
        if (!string.IsNullOrWhiteSpace(query.PatientName))
        {
            var name = query.PatientName.Trim();
            requests = requests.Where(r => (r.Patient!.FirstName + " " + r.Patient.LastName).Contains(name));
        }

        var rows = await requests.OrderByDescending(r => r.ExpectedAdmissionDate).ToListAsync(ct);

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("expectedAdmissionDate", "Expected Date"), new("patientUhid", "UHID"), new("patientName", "Patient"),
            new("patientType", "Patient Type"), new("department", "Department"), new("advisedDoctor", "Advised Doctor"),
            new("ward", "Ward"), new("status", "Status"), new("rejectionReason", "Rejection Reason"),
            new("admissionNumber", "IP No"), new("admissionDate", "Admission Date"), new("doctorName", "Doctor"), new("bedType", "Bed Type"),
        ];
        var reportRows = rows.Select(r =>
        {
            var deptNames = r.RequestedConsultant?.DoctorDepartments.Select(dd => dd.Department!.Name).Distinct().ToList() ?? [];
            return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["expectedAdmissionDate"] = r.ExpectedAdmissionDate.ToString("yyyy-MM-dd"),
                ["patientUhid"] = r.Patient!.Uhid,
                ["patientName"] = r.Patient.FirstName + " " + r.Patient.LastName,
                ["patientType"] = r.Patient.PatientCategory?.Name ?? "-",
                ["department"] = deptNames.Count > 0 ? string.Join(", ", deptNames) : "-",
                ["advisedDoctor"] = r.RequestedConsultant?.User?.FullName ?? "-",
                ["ward"] = r.Ward?.Name ?? "-",
                ["status"] = r.Status.ToString(),
                ["rejectionReason"] = r.RejectionReason ?? "-",
                ["admissionNumber"] = r.ApprovedAdmission?.AdmissionNumber ?? "-",
                ["admissionDate"] = r.ApprovedAdmission?.AdmissionDate.ToString("yyyy-MM-dd") ?? "-",
                ["doctorName"] = r.ApprovedAdmission?.PrimaryConsultant?.User?.FullName ?? "-",
                ["bedType"] = r.ApprovedAdmission?.CurrentBed?.Room?.RoomType?.Name ?? "-",
            };
        }).ToList();
        return new ReportResultDto("Doctor Admission Request Report", columns, reportRows);
    }

    private static string ValidateMode(string? mode)
    {
        var normalized = (mode ?? "summary").ToLowerInvariant();
        if (normalized is not ("summary" or "detail"))
        {
            throw new DomainException($"Unknown report mode '{mode}' — use 'summary' or 'detail'.");
        }
        return normalized;
    }

    private static DateTimeOffset AtStart(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
