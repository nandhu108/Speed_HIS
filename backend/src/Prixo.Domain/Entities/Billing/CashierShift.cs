using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Billing;

/// <summary>
/// Per-cashier-per-session cash reconciliation — the finer-grained "counter_sessions" the
/// plan called out and DayClose's own doc comment flagged as not modeled ("multiple
/// simultaneous cashier counters"). Sits alongside DayClose, not instead of it: DayClose is
/// still the single branch-day gate that blocks bill/payment creation; a CashierShift is
/// just a per-user lens over the same BillPayments (tagged via BillPayment.CashierShiftId)
/// for shift-level declared-vs-system reconciliation and a physical-cash handover step.
/// </summary>
public class CashierShift : BranchEntity
{
    public long UserId { get; set; }
    public User? User { get; set; }

    public DateTimeOffset OpenedAt { get; set; }
    public decimal OpeningFloat { get; set; }
    public CashierShiftStatus Status { get; set; } = CashierShiftStatus.Open;

    public DateTimeOffset? ClosedAt { get; set; }
    public string? Notes { get; set; }

    public long? ReceivedByUserId { get; set; }
    public User? ReceivedByUser { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }

    public ICollection<CashierShiftModeAmount> ModeAmounts { get; set; } = [];
}

/// <summary>Declared-vs-system breakdown per payment mode for one cashier shift — same shape as DayCloseModeAmount.</summary>
public class CashierShiftModeAmount : BaseEntity
{
    public long CashierShiftId { get; set; }
    public CashierShift? CashierShift { get; set; }
    public long PaymentModeId { get; set; }
    public PaymentMode? PaymentMode { get; set; }

    public decimal SystemAmount { get; set; }
    public decimal DeclaredAmount { get; set; }
    public decimal Variance { get; set; }
}
