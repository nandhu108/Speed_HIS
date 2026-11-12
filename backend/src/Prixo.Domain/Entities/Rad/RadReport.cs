using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;

namespace Prixo.Domain.Entities.Rad;

/// <summary>One per RadOrderItem — the entry→verify→approve pipeline mirrors Lab's LabResult exactly (status lives on RadOrderItem, this row just holds who/when plus the narrative).</summary>
public class RadReport : BaseEntity
{
    public long RadOrderItemId { get; set; }
    public RadOrderItem? RadOrderItem { get; set; }
    public long? TemplateId { get; set; }
    public RadReportTemplate? Template { get; set; }

    public required string Findings { get; set; }
    public required string Impression { get; set; }

    public long EnteredByUserId { get; set; }
    public User? EnteredByUser { get; set; }
    public DateTimeOffset EnteredAt { get; set; }
    public long? VerifiedByUserId { get; set; }
    public User? VerifiedByUser { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public long? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    /// <summary>Placeholder for the approver's digital signature image, same posture as Lab's LabResult.ApproverSignaturePath.</summary>
    public string? ApproverSignaturePath { get; set; }
}
