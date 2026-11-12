using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Biomed;

/// <summary>AMC/CMC contracts (plan §19). Renewal-due is computed on read (EndDate minus RenewalReminderDays), not stored — same "compute eligibility on read" posture MRD used for destruction/retention.</summary>
public class AmcContract : BranchEntity
{
    public long EquipmentId { get; set; }
    public BiomedEquipment? Equipment { get; set; }
    public long VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public ContractType ContractType { get; set; } = ContractType.Amc;
    public required string ContractNumber { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal Cost { get; set; }
    public int RenewalReminderDays { get; set; } = 30;
    public string? Notes { get; set; }
}
