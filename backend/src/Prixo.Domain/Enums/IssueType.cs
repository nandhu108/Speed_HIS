namespace Prixo.Domain.Enums;

/// <summary>
/// Department and Emergency issues are real, usable flows this phase. Ip is schema-ready
/// only — there's no admission/medication order to issue against until Phase 9 (IPD)
/// exists, same "schema now, generation path later" posture as Billing.BillType.Ip.
/// </summary>
public enum IssueType
{
    Department = 1,
    Emergency = 2,
    Ip = 3,
}
