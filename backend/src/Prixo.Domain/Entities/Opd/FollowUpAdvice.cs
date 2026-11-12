using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Opd;

/// <summary>
/// One follow-up recommendation per visit. Booking the actual appointment is a separate,
/// explicit action in the Appointments module (Phase 4 UI) — not auto-triggered here.
/// </summary>
public class FollowUpAdvice : BaseEntity
{
    public long VisitId { get; set; }
    public OpVisit? Visit { get; set; }

    public DateOnly FollowUpDate { get; set; }
    public string? Notes { get; set; }
}
