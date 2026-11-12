using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Lab;

/// <summary>Raised automatically when a ResultValue is flagged Critical; the ordering doctor/lab must acknowledge it.</summary>
public class CriticalAlert : BranchEntity
{
    public long LabResultId { get; set; }
    public LabResult? LabResult { get; set; }
    public long ResultValueId { get; set; }
    public ResultValue? ResultValue { get; set; }
    public DateTimeOffset RaisedAt { get; set; }
    public bool IsAcknowledged { get; set; }
    public long? AcknowledgedByUserId { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public string? AcknowledgementNotes { get; set; }
}
