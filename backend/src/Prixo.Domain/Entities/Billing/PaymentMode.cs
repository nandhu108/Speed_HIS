using Prixo.Domain.Common;
using Prixo.Domain.Entities.Finance;

namespace Prixo.Domain.Entities.Billing;

/// <summary>
/// Cash/Card/UPI/Cheque… global catalog, per the plan's "Payment modes (master)". Not
/// ISimpleMaster — RequiresReference needs dedicated handling, same reasoning as
/// Tax/DiscountType in Phase 2.
/// </summary>
public class PaymentMode : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    /// <summary>Card/UPI/Cheque need a reference number captured at collection time; Cash doesn't.</summary>
    public bool RequiresReference { get; set; }
    /// <summary>The Finance (Phase 18) bank/cash Account this mode's collections post to. Null until Finance configures it — BillingService's auto-posting hook skips silently when unset.</summary>
    public long? LinkedAccountId { get; set; }
    public Account? LinkedAccount { get; set; }
}
