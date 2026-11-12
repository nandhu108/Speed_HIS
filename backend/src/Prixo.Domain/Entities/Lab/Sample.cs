using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Lab;

/// <summary>
/// One specimen draw, covering every LabOrderItem of the same SampleType on that order
/// (a single blood draw can serve CBC + LFT + KFT). Rejection lives here rather than a
/// separate sample_rejections table — status + reason fields cover it, and AuditLog
/// already gives row-diff history for anything more.
/// </summary>
public class Sample : BranchEntity
{
    public long LabOrderId { get; set; }
    public LabOrder? LabOrder { get; set; }
    public long SampleTypeId { get; set; }
    public SampleType? SampleType { get; set; }
    public required string SampleNumber { get; set; }
    public SampleStatus Status { get; set; } = SampleStatus.Collected;
    public DateTimeOffset CollectedAt { get; set; }
    public long? CollectedByUserId { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
    public long? ReceivedByUserId { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public long? RejectedByUserId { get; set; }

    public ICollection<LabOrderItem> Items { get; set; } = [];
}
