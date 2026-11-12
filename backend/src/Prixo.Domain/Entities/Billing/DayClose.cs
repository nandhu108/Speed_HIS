using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Billing;

/// <summary>
/// Branch-and-date cash reconciliation. Closing a day blocks further bill/payment
/// creation for that (branch, date) — checked by BillingService before any money-moving
/// write. One counter/session per branch per day (the plan's finer-grained
/// "counter_sessions" — multiple simultaneous cashier counters — is not modeled; a single
/// day-level close per branch is the simplification here).
/// </summary>
public class DayClose : BranchEntity
{
    public DateOnly Date { get; set; }
    public DayCloseStatus Status { get; set; } = DayCloseStatus.Open;
    public DateTimeOffset? ClosedAt { get; set; }
    public long? ClosedByUserId { get; set; }
    public string? Notes { get; set; }

    public ICollection<DayCloseModeAmount> ModeAmounts { get; set; } = [];
}

/// <summary>Declared-vs-system breakdown per payment mode for one day close.</summary>
public class DayCloseModeAmount : BaseEntity
{
    public long DayCloseId { get; set; }
    public DayClose? DayClose { get; set; }
    public long PaymentModeId { get; set; }
    public PaymentMode? PaymentMode { get; set; }

    public decimal SystemAmount { get; set; }
    public decimal DeclaredAmount { get; set; }
    public decimal Variance { get; set; }
}
