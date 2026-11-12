using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Lab;

/// <summary>
/// One per LabOrderItem. No separate status field — LabOrderItem.Status (ResultEntered/
/// Verified/Approved) is the single source of truth for the pipeline; this row just holds
/// who/when for each step plus the entered values.
/// </summary>
public class LabResult : BaseEntity
{
    public long LabOrderItemId { get; set; }
    public LabOrderItem? LabOrderItem { get; set; }
    public long EnteredByUserId { get; set; }
    public DateTimeOffset EnteredAt { get; set; }
    public long? VerifiedByUserId { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public long? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? Notes { get; set; }
    /// <summary>Denormalized for fast worklist/critical-list filtering — true if any ResultValue.Flag is Critical.</summary>
    public bool HasCriticalValue { get; set; }
    /// <summary>Placeholder for the approver's digital signature image (plan §11 Phase 7: "signature image; DSC later") — column exists, nothing writes to it yet.</summary>
    public string? ApproverSignaturePath { get; set; }

    public ICollection<ResultValue> Values { get; set; } = [];
}

/// <summary>
/// One row per LabTestParameter within a result. Unit/NormalLow/NormalHigh are
/// snapshotted at entry time — same reasoning as Billing's BillItem snapshot fields, so a
/// historical report still reads correctly if the reference range changes later.
/// </summary>
public class ResultValue : BaseEntity
{
    public long LabResultId { get; set; }
    public LabResult? LabResult { get; set; }
    public long LabTestParameterId { get; set; }
    public LabTestParameter? LabTestParameter { get; set; }
    public required string Value { get; set; }
    public string? Unit { get; set; }
    public decimal? NormalLow { get; set; }
    public decimal? NormalHigh { get; set; }
    public ResultFlag Flag { get; set; } = ResultFlag.Normal;
}
