namespace Prixo.Domain.Enums;

public enum BillItemSourceType
{
    Consultation = 1,
    ClinicalOrder = 2,
    Service = 3,
    Package = 4,
    /// <summary>Auto-managed IP bed-charge line (Phase 9) — recomputed from BedAllocation history, not manually added.</summary>
    BedCharge = 5,
    /// <summary>Implant/consumable usage posted by OtUsageService (Phase 14) — theatre time/surgeon/anaesthesia fees post as ordinary Service lines instead, reusing tariff resolution.</summary>
    OtCharge = 6,
    /// <summary>Medicine issued by the pharmacy to an inpatient (IssueService), billed at MRP (tax-inclusive).</summary>
    Pharmacy = 7,
    /// <summary>Lab test pulled from a lab order onto the bill.</summary>
    LabTest = 8,
    /// <summary>Radiology scan pulled from a radiology order onto the bill.</summary>
    RadiologyTest = 9,
}
