namespace Prixo.Domain.Enums;

public enum BillType
{
    Op = 1,
    Ip = 2,
    /// <summary>Post-facto friendly (plan §16) — generated empty against an EmergencyVisit, then billed via the same manual-item path as everything else.</summary>
    Emergency = 3,
}
