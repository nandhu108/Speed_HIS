namespace Prixo.Domain.Enums;

/// <summary>The plan's fixed 4-way discharge clearance checklist (§9). One row per type per admission.</summary>
public enum ClearanceType
{
    Billing = 1,
    Pharmacy = 2,
    Nursing = 3,
    FinalInvestigation = 4,
}
