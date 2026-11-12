using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Pharmacy;

/// <summary>
/// A department's request against a store (plan §13 "indent → approve → issue") — sits in
/// front of the existing Issue engine rather than duplicating it. Approving sets each item's
/// ApprovedQuantity (defaults to RequestedQuantity if the approver doesn't trim it);
/// fulfilling is a normal Issue with IndentId set, which IssueService validates against the
/// approved quantities and then marks this Issued.
/// </summary>
public class Indent : BranchEntity
{
    public long StoreId { get; set; }
    public Store? Store { get; set; }
    public long DepartmentId { get; set; }
    public Department? Department { get; set; }
    public required string IndentNumber { get; set; }
    public DateOnly IndentDate { get; set; }
    public IndentStatus Status { get; set; } = IndentStatus.Requested;
    public string? Notes { get; set; }

    public long RequestedByUserId { get; set; }
    public User? RequestedByUser { get; set; }
    public long? DecidedByUserId { get; set; }
    public User? DecidedByUser { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? RejectionReason { get; set; }

    public ICollection<IndentItem> Items { get; set; } = [];
}

/// <summary>Exactly one of DrugId/ItemId is set — see Batch's doc comment for the pattern.</summary>
public class IndentItem : BaseEntity
{
    public long IndentId { get; set; }
    public Indent? Indent { get; set; }
    public long? DrugId { get; set; }
    public Drug? Drug { get; set; }
    public long? ItemId { get; set; }
    public Item? Item { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal? ApprovedQuantity { get; set; }
}
