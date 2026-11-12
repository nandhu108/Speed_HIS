using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Nursing;

/// <summary>
/// Incident reporting (plan §10) — and, since Phase 20 (Quality/NABH), the org-wide incident
/// register: rather than forking a parallel table, Quality's RCA/sentinel-event tracking widens
/// this entity in place (same "widen, don't fork" posture as ICU widening IpVitals or Stores
/// widening Batch). Patient/Admission are optional — an incident can be staff/visitor/general
/// (needle-stick, fall in corridor…) with no patient involved. Branch-scoped directly like
/// ShiftHandover.
/// </summary>
public class Incident : BranchEntity
{
    public DateTimeOffset IncidentDate { get; set; }
    public required string IncidentType { get; set; }
    public required string Location { get; set; }
    public IncidentSeverity Severity { get; set; }
    public required string Description { get; set; }
    public string? ImmediateActionTaken { get; set; }

    public long? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public long? AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public long ReportedByUserId { get; set; }
    public User? ReportedByUser { get; set; }

    public IncidentStatus Status { get; set; } = IncidentStatus.Reported;
    public string? ReviewNotes { get; set; }
    public long? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    /// <summary>Which module reported this — e.g. "NURSING", "QUALITY". Nullable: existing rows and most callers don't set it.</summary>
    public string? SourceModule { get; set; }
    /// <summary>Set by Quality's RCA action (QUALITY.INCIDENT.RCA) — not part of Nursing's own Report/Review/Close flow.</summary>
    public bool IsSentinelEvent { get; set; }
    public string? RootCauseAnalysis { get; set; }
}
