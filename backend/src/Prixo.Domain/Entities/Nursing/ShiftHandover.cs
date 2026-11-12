using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Nursing;

/// <summary>
/// Ward-level, per-shift handover header (plan §10 "SBAR-style, per ward"). Branch-scoped
/// directly (like Admission/Bill) rather than only through Ward, so it lists/filters without
/// an extra join — same posture as other top-level transactional entities.
/// </summary>
public class ShiftHandover : BranchEntity
{
    public long WardId { get; set; }
    public Ward? Ward { get; set; }

    public DateOnly ShiftDate { get; set; }
    public NursingShift Shift { get; set; }

    public long HandoverFromUserId { get; set; }
    public User? HandoverFromUser { get; set; }
    public long? HandoverToUserId { get; set; }
    public User? HandoverToUser { get; set; }

    public string? GeneralNotes { get; set; }
    public DateTimeOffset HandedOverAt { get; set; }

    public ICollection<PatientHandoverNote> PatientNotes { get; set; } = [];
}

/// <summary>Per-patient SBAR line within a ShiftHandover.</summary>
public class PatientHandoverNote : BaseEntity
{
    public long ShiftHandoverId { get; set; }
    public ShiftHandover? ShiftHandover { get; set; }

    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public required string Situation { get; set; }
    public required string Background { get; set; }
    public required string Assessment { get; set; }
    public required string Recommendation { get; set; }
}
