using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;

namespace Prixo.Domain.Entities.Icu;

/// <summary>A CriticalAlertRule breach raised against one IpVitals reading — same acknowledge workflow shape as Lab's CriticalAlert, a genuinely different domain (vitals vs. lab result values) so it's its own entity rather than a forced reuse.</summary>
public class IcuVitalsAlert : BranchEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }
    public long IpVitalsId { get; set; }
    public IpVitals? IpVitals { get; set; }
    public long CriticalAlertRuleId { get; set; }
    public CriticalAlertRule? CriticalAlertRule { get; set; }

    public DateTimeOffset RaisedAt { get; set; }
    public bool IsAcknowledged { get; set; }
    public long? AcknowledgedByUserId { get; set; }
    public User? AcknowledgedByUser { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public string? AcknowledgementNotes { get; set; }
}
