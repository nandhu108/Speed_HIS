using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;

namespace Prixo.Domain.Entities.Mrd;

/// <summary>
/// Discharge File Audit (plan §17: "checklist: summary signed, consents present...") plus
/// Missing Document Tracking. One row per Admission (upsert, same idiom as EmergencyAssessment)
/// — <see cref="IsComplete"/> is recomputed by the service every save from the checklist flags.
/// </summary>
public class FileAudit : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public bool DischargeSummarySigned { get; set; }
    public bool ConsentsPresent { get; set; }
    public bool ProgressNotesComplete { get; set; }
    public bool InvestigationReportsPresent { get; set; }
    public bool NursingRecordsComplete { get; set; }
    public bool IsComplete { get; set; }
    public string? MissingDocumentsNotes { get; set; }

    public long AuditedByUserId { get; set; }
    public User? AuditedByUser { get; set; }
    public DateTimeOffset AuditedAt { get; set; }
}
