using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Masters;

/// <summary>
/// Doctor fee/commission share definition per service category (e.g. "Dr. Rao gets 40% of
/// Investigation charges"), backing the Doctors Payout / Performing Doctor reports and the
/// Doctor Fee Control Panel. Branch-scoped — payout terms are negotiated per unit, same as
/// Tariff. Report-only: no ledger posting, since payroll/HR is a held module (plan §21).
/// </summary>
public class DoctorPayoutRule : BranchEntity
{
    public long DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    public long ServiceCategoryId { get; set; }
    public ServiceCategory? ServiceCategory { get; set; }
    public PayoutType PayoutType { get; set; } = PayoutType.Percent;
    /// <summary>Percent (0-100) when PayoutType is Percent; a flat currency amount per billed line when Flat.</summary>
    public decimal Value { get; set; }
}
