using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Pharmacy;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Nursing;

/// <summary>
/// The action taken against a MarSchedule row — administer/skip/refuse (plan §10). VerifiedByUserId
/// is required by MarService whenever the schedule row is IsHighAlert (the "double-verification"
/// rule); it stays optional here since the DB doesn't know which drugs are high-alert.
///
/// BatchId/Quantity (added post-Phase-10) let an Administered dose optionally decrement Pharmacy
/// stock — nullable because a nurse must always be able to record giving a dose even when no
/// batch is picked (clinical charting can never be blocked by inventory bookkeeping). When set,
/// MarService writes an IssueOut StockLedger row against the batch, the same "document is its
/// own reference" idiom OT's ImplantUsage/ConsumableUsage already use.
/// </summary>
public class MarAdministration : BaseEntity
{
    public long MarScheduleId { get; set; }
    public MarSchedule? MarSchedule { get; set; }

    public MarStatus Action { get; set; }
    public DateTimeOffset ActionAt { get; set; }
    public long ActionByUserId { get; set; }
    public User? ActionByUser { get; set; }

    public long? VerifiedByUserId { get; set; }
    public User? VerifiedByUser { get; set; }

    public string? Reason { get; set; }
    public string? Notes { get; set; }

    public long? BatchId { get; set; }
    public Batch? Batch { get; set; }
    public decimal? Quantity { get; set; }
}
