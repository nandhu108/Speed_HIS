using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Emr;

/// <summary>
/// Patient Timeline and the dedicated history views (plan §11) — pure aggregation over
/// existing data, no new storage. Each source is queried separately (OpVisits, Admissions,
/// approved LabResults, Prescriptions, IPD ProcedureNotes, DischargeSummaries, ClinicalNotes,
/// Attachments) then merged and sorted in memory; at this scale (one patient's history) that's
/// simpler and cheap enough that a single UNIONed SQL query isn't worth the complexity.
/// </summary>
public class TimelineService(IAppDbContext db)
{
    public async Task<IReadOnlyList<TimelineEventDto>> GetTimelineAsync(long patientId, CancellationToken ct = default)
    {
        await RequirePatientAsync(patientId, ct);
        var events = new List<TimelineEventDto>();

        var visits = await db.OpVisits.AsNoTracking().Where(v => v.PatientId == patientId)
            .Include(v => v.Doctor).ThenInclude(d => d!.User)
            .Select(v => new { v.Id, v.VisitDate, v.CompletedAt, v.VisitType, v.Status, DoctorName = v.Doctor!.User!.FullName })
            .ToListAsync(ct);
        events.AddRange(visits.Select(v => new TimelineEventDto(
            v.CompletedAt ?? new DateTimeOffset(v.VisitDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            TimelineEventType.Visit, $"OPD Visit — {v.DoctorName}", $"{v.VisitType} · {v.Status}", v.Id)));

        var admissions = await db.Admissions.AsNoTracking().Where(a => a.PatientId == patientId)
            .Select(a => new { a.Id, a.AdmissionDate, a.AdmissionNumber, a.AdmissionType, a.Status })
            .ToListAsync(ct);
        events.AddRange(admissions.Select(a => new TimelineEventDto(
            a.AdmissionDate, TimelineEventType.Admission, $"Admission — {a.AdmissionNumber}", $"{a.AdmissionType} · {a.Status}", a.Id)));

        var labResults = await db.LabOrderItems.AsNoTracking()
            .Where(i => i.LabOrder!.PatientId == patientId && i.Status == LabOrderItemStatus.Approved)
            .Include(i => i.LabTest)
            .Select(i => new { i.Id, i.LabTest!.Name, i.Result!.ApprovedAt })
            .ToListAsync(ct);
        events.AddRange(labResults.Where(r => r.ApprovedAt is not null).Select(r => new TimelineEventDto(
            r.ApprovedAt!.Value, TimelineEventType.LabResult, $"Lab Result — {r.Name}", "Approved", r.Id)));

        var radReports = await db.RadOrderItems.AsNoTracking()
            .Where(i => i.RadOrder!.PatientId == patientId && i.Status == RadOrderItemStatus.Approved && i.Report != null)
            .Select(i => new { i.Id, i.RadiologyTest!.Name, i.Report!.Impression, i.Report.ApprovedAt })
            .ToListAsync(ct);
        events.AddRange(radReports.Where(r => r.ApprovedAt is not null).Select(r => new TimelineEventDto(
            r.ApprovedAt!.Value, TimelineEventType.RadiologyReport, $"Radiology Report — {r.Name}", r.Impression, r.Id)));

        var prescriptions = await db.Prescriptions.AsNoTracking().Where(p => p.Visit!.PatientId == patientId)
            .Include(p => p.Visit).ThenInclude(v => v!.Doctor).ThenInclude(d => d!.User)
            .Include(p => p.Items).ThenInclude(i => i.Drug)
            .Select(p => new { p.Id, p.Visit!.VisitDate, p.Visit.CompletedAt, DoctorName = p.Visit.Doctor!.User!.FullName, DrugNames = p.Items.Select(i => i.Drug!.Name) })
            .ToListAsync(ct);
        events.AddRange(prescriptions.Select(p => new TimelineEventDto(
            p.CompletedAt ?? new DateTimeOffset(p.VisitDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            TimelineEventType.Prescription, $"Prescription — {p.DoctorName}", string.Join(", ", p.DrugNames), p.Id)));

        var procedures = await db.ProcedureNotes.AsNoTracking().Where(n => n.Admission!.PatientId == patientId)
            .Select(n => new { n.Id, n.ProcedureName, n.PerformedAt })
            .ToListAsync(ct);
        events.AddRange(procedures.Select(n => new TimelineEventDto(n.PerformedAt, TimelineEventType.Procedure, $"Procedure — {n.ProcedureName}", string.Empty, n.Id)));

        var summaries = await db.DischargeSummaries.AsNoTracking().Where(s => s.Admission!.PatientId == patientId)
            .Select(s => new { s.Id, s.Admission!.DischargedAt, s.Admission.AdmissionDate, s.FinalDiagnosis, s.IsApproved })
            .ToListAsync(ct);
        events.AddRange(summaries.Select(s => new TimelineEventDto(
            s.DischargedAt ?? s.AdmissionDate, TimelineEventType.DischargeSummary,
            "Discharge Summary", s.FinalDiagnosis, s.Id)));

        var notes = await db.ClinicalNotes.AsNoTracking().Where(n => n.PatientId == patientId)
            .Select(n => new { n.Id, n.NoteType, n.Title, n.NoteAt })
            .ToListAsync(ct);
        events.AddRange(notes.Select(n => new TimelineEventDto(n.NoteAt, TimelineEventType.ClinicalNote, $"Clinical Note — {n.NoteType}", n.Title ?? string.Empty, n.Id)));

        var attachments = await db.Attachments.AsNoTracking().Where(a => a.PatientId == patientId)
            .Select(a => new { a.Id, a.AttachmentType, a.FileName, a.UploadedAt })
            .ToListAsync(ct);
        events.AddRange(attachments.Select(a => new TimelineEventDto(a.UploadedAt, TimelineEventType.Attachment, $"Attachment — {a.AttachmentType}", a.FileName, a.Id)));

        return [.. events.OrderByDescending(e => e.At)];
    }

    public async Task<IReadOnlyList<PrescriptionHistoryDto>> GetPrescriptionHistoryAsync(long patientId, CancellationToken ct = default)
    {
        await RequirePatientAsync(patientId, ct);
        var prescriptions = await db.Prescriptions.AsNoTracking().Where(p => p.Visit!.PatientId == patientId)
            .Include(p => p.Visit).ThenInclude(v => v!.Doctor).ThenInclude(d => d!.User)
            .Include(p => p.Items).ThenInclude(i => i.Drug)
            .OrderByDescending(p => p.Visit!.VisitDate)
            .ToListAsync(ct);

        return [.. prescriptions.Select(p => new PrescriptionHistoryDto(
            p.VisitId, p.Visit!.VisitDate, p.Visit.Doctor!.User!.FullName,
            [.. p.Items.Select(i => new PrescriptionHistoryItemDto(i.DrugId, i.Drug!.Name, i.Dose, i.Frequency, i.Duration))]))];
    }

    public async Task<IReadOnlyList<InvestigationHistoryDto>> GetInvestigationHistoryAsync(long patientId, CancellationToken ct = default)
    {
        await RequirePatientAsync(patientId, ct);
        var items = await db.LabOrderItems.AsNoTracking().Where(i => i.LabOrder!.PatientId == patientId)
            .Include(i => i.LabOrder)
            .Include(i => i.LabTest)
            .Include(i => i.Result).ThenInclude(r => r!.Values)
            .OrderByDescending(i => i.LabOrder!.OrderDate)
            .ToListAsync(ct);

        var labRows = items.Select(i => new InvestigationHistoryDto(
            i.Id, i.LabOrderId, i.LabOrder!.OrderDate, i.LabTest!.Name, i.Status.ToString(),
            i.Status == LabOrderItemStatus.Approved && i.Result is not null
                ? string.Join(", ", i.Result.Values.Select(v => $"{v.Value}{(string.IsNullOrEmpty(v.Unit) ? "" : $" {v.Unit}")}"))
                : null));

        // Imaging belongs in the same investigations view: approved reports show their impression.
        var scans = await db.RadOrderItems.AsNoTracking().Where(i => i.RadOrder!.PatientId == patientId)
            .Select(i => new { i.Id, i.RadOrderId, i.RadOrder!.OrderDate, i.RadiologyTest!.Name, i.Status, Impression = i.Report != null ? i.Report.Impression : null })
            .ToListAsync(ct);
        var radRows = scans.Select(s => new InvestigationHistoryDto(
            s.Id, s.RadOrderId, s.OrderDate, $"Radiology — {s.Name}", s.Status.ToString(),
            s.Status == RadOrderItemStatus.Approved ? s.Impression : null));

        return [.. labRows.Concat(radRows).OrderByDescending(r => r.OrderDate)];
    }

    public async Task<IReadOnlyList<ProcedureHistoryDto>> GetProcedureHistoryAsync(long patientId, CancellationToken ct = default)
    {
        await RequirePatientAsync(patientId, ct);
        var procedures = await db.ProcedureNotes.AsNoTracking().Where(n => n.Admission!.PatientId == patientId)
            .Include(n => n.Admission).Include(n => n.PerformedByDoctor).ThenInclude(d => d!.User)
            .OrderByDescending(n => n.PerformedAt)
            .ToListAsync(ct);

        return [.. procedures.Select(n => new ProcedureHistoryDto(
            n.AdmissionId, n.Admission!.AdmissionNumber, n.ProcedureName, n.PerformedAt, n.PerformedByDoctor!.User!.FullName, n.Findings))];
    }

    public async Task<IReadOnlyList<DischargeSummaryArchiveDto>> GetDischargeSummaryArchiveAsync(long patientId, CancellationToken ct = default)
    {
        await RequirePatientAsync(patientId, ct);
        return await db.DischargeSummaries.AsNoTracking().Where(s => s.Admission!.PatientId == patientId)
            .Include(s => s.Admission)
            .OrderByDescending(s => s.Admission!.AdmissionDate)
            .Select(s => new DischargeSummaryArchiveDto(s.AdmissionId, s.Admission!.AdmissionNumber, s.Admission.DischargedAt, s.FinalDiagnosis, s.IsApproved))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ResultsTrendSeriesDto>> GetResultsTrendAsync(long patientId, long testId, CancellationToken ct = default)
    {
        await RequirePatientAsync(patientId, ct);
        var values = await db.ResultValues.AsNoTracking()
            .Where(v => v.LabResult!.LabOrderItem!.LabOrder!.PatientId == patientId
                && v.LabResult.LabOrderItem.LabTestId == testId
                && v.LabResult.LabOrderItem.Status == LabOrderItemStatus.Approved)
            .Include(v => v.LabTestParameter)
            .Select(v => new
            {
                v.LabTestParameterId, v.LabTestParameter!.Name, v.LabTestParameter.Unit, v.LabTestParameter.SortOrder,
                v.Value, v.Flag, OrderDate = v.LabResult!.LabOrderItem!.LabOrder!.OrderDate,
            })
            .ToListAsync(ct);

        return [.. values.GroupBy(v => new { v.LabTestParameterId, v.Name, v.Unit, v.SortOrder })
            .OrderBy(g => g.Key.SortOrder)
            .Select(g => new ResultsTrendSeriesDto(
                g.Key.LabTestParameterId, g.Key.Name, g.Key.Unit,
                [.. g.Select(v => (v.OrderDate, Value: EmrMath.TryParseTrendValue(v.Value), v.Flag))
                    .Where(v => v.Value is not null)
                    .OrderBy(v => v.OrderDate)
                    .Select(v => new ResultsTrendPointDto(v.OrderDate, v.Value!.Value, v.Flag.ToString()))]))];
    }

    private async Task RequirePatientAsync(long patientId, CancellationToken ct)
    {
        if (!await db.Patients.AnyAsync(p => p.Id == patientId, ct))
        {
            throw new NotFoundException(nameof(Patient), patientId);
        }
    }
}
