using Prixo.Domain.Common;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Pharmacy;

/// <summary>
/// Non-sale stock issue. Department/Emergency are real usable flows (issued against a
/// Department, no upstream order needed); Ip now carries an optional AdmissionId (Phase 9)
/// — still manual (nothing auto-generates one from a MedicationOrder yet, that's Phase 10's
/// MAR), but a pharmacist can now issue against an admission and have it show up in that
/// stay's consumption.
/// </summary>
public class Issue : BranchEntity
{
    public long StoreId { get; set; }
    public Store? Store { get; set; }
    public IssueType IssueType { get; set; }
    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public long? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public long? AdmissionId { get; set; }
    public Admission? Admission { get; set; }
    /// <summary>Set when this issue fulfils an approved Indent (Phase 13) — traceability only, nothing requires an issue to come from one.</summary>
    public long? IndentId { get; set; }
    public Indent? Indent { get; set; }
    public required string IssueNumber { get; set; }
    public DateOnly IssueDate { get; set; }
    public string? Notes { get; set; }

    public ICollection<IssueItem> Items { get; set; } = [];
}

public class IssueItem : BaseEntity
{
    public long IssueId { get; set; }
    public Issue? Issue { get; set; }
    public long BatchId { get; set; }
    public Batch? Batch { get; set; }
    public decimal Quantity { get; set; }
    /// <summary>The IP bill line this issue was charged on (IP issues only); a return reduces that line.</summary>
    public long? BillItemId { get; set; }
}

public class IssueReturn : BranchEntity
{
    public long IssueId { get; set; }
    public Issue? Issue { get; set; }
    public required string ReturnNumber { get; set; }
    public DateOnly ReturnDate { get; set; }
    public required string Reason { get; set; }

    public ICollection<IssueReturnItem> Items { get; set; } = [];
}

public class IssueReturnItem : BaseEntity
{
    public long IssueReturnId { get; set; }
    public IssueReturn? IssueReturn { get; set; }
    public long IssueItemId { get; set; }
    public IssueItem? IssueItem { get; set; }
    public decimal Quantity { get; set; }
}
