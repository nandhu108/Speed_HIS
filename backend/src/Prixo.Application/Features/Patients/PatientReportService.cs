using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Reports;

namespace Prixo.Application.Features.Patients;

/// <summary>"Patient Block/Unblock Report" — every PATIENT_BLOCKED/PATIENT_UNBLOCKED activity-log
/// event in a date range, exported via the shared <see cref="ReportExportHelper"/>. Reuses the
/// existing activity log (`PatientService.BlockAsync`/`UnblockAsync` already write it, with the
/// patient UHID/reason baked into `Description`) rather than a new history table — the only
/// thing genuinely missing before this was the export layer.</summary>
public class PatientReportService(IAppDbContext db)
{
    public async Task<ReportResultDto> GetBlockUnblockReportAsync(ReportExecuteRequest query, CancellationToken ct = default)
    {
        var logs = db.ActivityLogs.AsNoTracking()
            .Where(l => l.EventType == ActivityEvents.PatientBlocked || l.EventType == ActivityEvents.PatientUnblocked);
        if (query.FromDate is { } from)
        {
            var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            logs = logs.Where(l => l.CreatedAt >= start);
        }
        if (query.ToDate is { } to)
        {
            var end = new DateTimeOffset(to.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(1);
            logs = logs.Where(l => l.CreatedAt < end);
        }

        var rows = await logs.OrderByDescending(l => l.CreatedAt).ToListAsync(ct);

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("dateTime", "Date/Time"), new("action", "Action"), new("byUser", "By"), new("details", "Details"),
        ];
        var reportRows = rows.Select(l => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["dateTime"] = l.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            ["action"] = l.EventType == ActivityEvents.PatientBlocked ? "Blocked" : "Unblocked",
            ["byUser"] = l.Username ?? "—",
            ["details"] = l.Description,
        }).ToList();
        return new ReportResultDto("Patient Block/Unblock Report", columns, reportRows);
    }
}
