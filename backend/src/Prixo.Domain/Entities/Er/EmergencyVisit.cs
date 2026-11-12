using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Er;

/// <summary>
/// The ER anchor, mirroring OpVisit's role for OPD (plan §16: "reuses OPD/IPD/lab/billing
/// engines"). PatientId always points at a real Patient row — "unknown-patient support"
/// isn't a schema feature, it's a usage pattern: registration uses Phase 3's existing
/// minimal-required-fields RegisterAsync with placeholder values (e.g. "Unknown"/"Unknown"),
/// and "later merge" is Phase 3's existing duplicate-detection + merge flow once identity is
/// confirmed — no new Patient capability was needed. AssignedDoctorId is set once triage or
/// assessment picks a doctor; nothing requires one at registration time.
/// </summary>
public class EmergencyVisit : BranchEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }
    public long? AssignedDoctorId { get; set; }
    public Doctor? AssignedDoctor { get; set; }

    public required string VisitNumber { get; set; }
    public int TokenNumber { get; set; }
    public DateTimeOffset ArrivedAt { get; set; }
    public ArrivalMode ArrivalMode { get; set; } = ArrivalMode.WalkIn;
    public bool IsMlc { get; set; }

    public EmergencyStatus Status { get; set; } = EmergencyStatus.Registered;
    public long RegisteredByUserId { get; set; }
    public User? RegisteredByUser { get; set; }

    public string? CancelReason { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
}
