using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Mrd;

/// <summary>MRD Reports (plan §17: "coding stats, file movement, incomplete records") — read-only aggregates over the module's own tables plus IPD's Admission.</summary>
public class MrdReportService(IAppDbContext db)
{
    public async Task<CodingStatsDto> GetCodingStatsAsync(MrdReportDateRange range, CancellationToken ct = default)
    {
        var admissions = db.Admissions.AsNoTracking().Where(a => a.Status == AdmissionStatus.Discharged);
        admissions = ApplyDischargeRange(admissions, range);

        var total = await admissions.CountAsync(ct);
        var coded = await admissions.Where(a => db.CodingWorklistEntries.Any(c => c.AdmissionId == a.Id && c.Status == CodingStatus.Coded)).CountAsync(ct);
        var pending = total - coded;
        var percent = total == 0 ? 0m : Math.Round(coded * 100m / total, 1);
        return new CodingStatsDto(total, coded, pending, percent);
    }

    public async Task<IReadOnlyList<FileMovementReportRowDto>> GetFileMovementReportAsync(MrdReportDateRange range, CancellationToken ct = default)
    {
        var movements = db.FileMovements.AsNoTracking()
            .Include(m => m.RecordFile).ThenInclude(f => f!.Admission).ThenInclude(a => a!.Patient)
            .Include(m => m.MovedByUser)
            .AsQueryable();
        if (range.FromDate is { } from)
        {
            var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            movements = movements.Where(m => m.MovedAt >= start);
        }
        if (range.ToDate is { } to)
        {
            var end = new DateTimeOffset(to.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(1);
            movements = movements.Where(m => m.MovedAt < end);
        }

        return await movements.OrderByDescending(m => m.MovedAt)
            .Select(m => new FileMovementReportRowDto(
                m.MovedAt, m.RecordFile!.FileNumber, m.RecordFile.Admission!.Patient!.FirstName + " " + m.RecordFile.Admission.Patient.LastName,
                m.FromLocation, m.ToLocation, m.MovedByUser!.FullName))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<IncompleteRecordRowDto>> GetIncompleteRecordsAsync(MrdReportDateRange range, CancellationToken ct = default)
    {
        var admissions = db.Admissions.AsNoTracking().Where(a => a.Status == AdmissionStatus.Discharged);
        admissions = ApplyDischargeRange(admissions, range);

        var rows = await admissions.Include(a => a.Patient)
            .Select(a => new
            {
                Admission = a,
                HasRecordFile = db.RecordFiles.Any(f => f.AdmissionId == a.Id),
                Audit = db.FileAudits.FirstOrDefault(f => f.AdmissionId == a.Id),
                IsCoded = db.CodingWorklistEntries.Any(c => c.AdmissionId == a.Id && c.Status == CodingStatus.Coded),
            })
            .ToListAsync(ct);

        return rows
            .Where(x => !x.HasRecordFile || x.Audit is null || !x.Audit.IsComplete || !x.IsCoded)
            .Select(x => new IncompleteRecordRowDto(
                x.Admission.Id, x.Admission.AdmissionNumber, x.Admission.Patient!.FirstName + " " + x.Admission.Patient.LastName,
                x.Admission.DischargedAt, x.HasRecordFile, x.Audit is not null, x.Audit?.IsComplete ?? false, x.IsCoded))
            .OrderByDescending(x => x.DischargedAt)
            .ToList();
    }

    private static IQueryable<Domain.Entities.Ipd.Admission> ApplyDischargeRange(IQueryable<Domain.Entities.Ipd.Admission> query, MrdReportDateRange range)
    {
        if (range.FromDate is { } from)
        {
            var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(a => a.DischargedAt >= start);
        }
        if (range.ToDate is { } to)
        {
            var end = new DateTimeOffset(to.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(1);
            query = query.Where(a => a.DischargedAt < end);
        }
        return query;
    }
}
