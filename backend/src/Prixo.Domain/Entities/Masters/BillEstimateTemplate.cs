using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Masters;

/// <summary>
/// A reusable set of Service/Package lines (e.g. "Normal Delivery Package Estimate") that
/// seeds a Billing.BillEstimate — the pre-admission cost-estimate counselling tool.
/// Branch-scoped, same posture as Package (prices differ per unit).
/// </summary>
public class BillEstimateTemplate : BranchEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public ICollection<BillEstimateTemplateItem> Items { get; set; } = [];
}

public class BillEstimateTemplateItem : BaseEntity
{
    public long BillEstimateTemplateId { get; set; }
    public BillEstimateTemplate? BillEstimateTemplate { get; set; }
    public BillItemSourceType SourceType { get; set; }
    public long? ServiceId { get; set; }
    public Service? Service { get; set; }
    public long? PackageId { get; set; }
    public Package? Package { get; set; }
    public decimal Quantity { get; set; } = 1;
}
