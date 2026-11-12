using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Ipd;

/// <summary>
/// One row per ClearanceType, auto-created when discharge is requested (DischargeService).
/// Final discharge is blocked until every row IsCleared — the plan's 4-way checklist.
/// </summary>
public class DischargeClearance : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public ClearanceType ClearanceType { get; set; }
    public bool IsCleared { get; set; }
    public long? ClearedByUserId { get; set; }
    public DateTimeOffset? ClearedAt { get; set; }
    public string? Remarks { get; set; }
}
