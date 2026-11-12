using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Billing;

/// <summary>
/// A bill line. Description is a snapshot (not just a Service lookup) so historical
/// bills read correctly even if the catalog entry is later renamed. Rate/tax are
/// snapshotted too, resolved at add-time from the active Tariff (plan's "rate revision").
/// </summary>
public class BillItem : BaseEntity
{
    public long BillId { get; set; }
    public Bill? Bill { get; set; }

    public BillItemSourceType SourceType { get; set; }
    public long? ServiceId { get; set; }
    public Service? Service { get; set; }
    public long? PackageId { get; set; }
    public Package? Package { get; set; }
    /// <summary>Set when SourceType is ClinicalOrder — links the bill line back to the OPD order.</summary>
    public long? ClinicalOrderId { get; set; }
    public ClinicalOrder? ClinicalOrder { get; set; }

    public required string Description { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal Rate { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
}
