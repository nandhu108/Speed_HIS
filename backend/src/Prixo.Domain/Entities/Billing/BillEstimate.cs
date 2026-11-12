using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Billing;

/// <summary>
/// A pre-admission/pre-procedure cost estimate — purely informational counselling tool,
/// generated from a BillEstimateTemplate or built ad hoc. Never auto-converts into a real
/// Bill; front desk creates that separately later once the patient proceeds, matching the
/// codebase's manual-refresh house style (e.g. IPD's RefreshBedChargesAsync).
/// </summary>
public class BillEstimate : BranchEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }
    public long? BillEstimateTemplateId { get; set; }
    public BillEstimateTemplate? BillEstimateTemplate { get; set; }

    public required string EstimateNumber { get; set; }
    public DateOnly EstimateDate { get; set; }
    public BillEstimateStatus Status { get; set; } = BillEstimateStatus.Draft;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }

    public ICollection<BillEstimateItem> Items { get; set; } = [];
}

/// <summary>A snapshot line — Description/Rate captured at estimate time, same posture as BillItem.</summary>
public class BillEstimateItem : BaseEntity
{
    public long BillEstimateId { get; set; }
    public BillEstimate? BillEstimate { get; set; }

    public BillItemSourceType SourceType { get; set; }
    public long? ServiceId { get; set; }
    public Service? Service { get; set; }
    public long? PackageId { get; set; }
    public Package? Package { get; set; }

    public required string Description { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal Rate { get; set; }
    public decimal TotalAmount { get; set; }
}
