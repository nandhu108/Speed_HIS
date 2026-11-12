using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Billing;

/// <summary>
/// A post-finalization value adjustment against a Paid/PartiallyPaid bill — reverses specific
/// line-item amounts without touching PaidAmount (unlike Refund, which returns cash). If the
/// adjustment leaves the patient overpaid, that excess is returned separately through the
/// existing Refund workflow — Credit Note only corrects what was billed.
/// </summary>
public class BillCreditNote : BaseEntity
{
    public long BillId { get; set; }
    public Bill? Bill { get; set; }

    public required string CreditNoteNumber { get; set; }
    public DateOnly CreditNoteDate { get; set; }
    public required string Reason { get; set; }
    public decimal Amount { get; set; }
    public CreditNoteStatus Status { get; set; } = CreditNoteStatus.Pending;
    public long? DecidedByUserId { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? RejectionReason { get; set; }

    public ICollection<BillCreditNoteItem> Items { get; set; } = [];
}

/// <summary>A reversed portion of one bill line. Amount is capped at that line's TotalAmount at request time.</summary>
public class BillCreditNoteItem : BaseEntity
{
    public long BillCreditNoteId { get; set; }
    public BillCreditNote? BillCreditNote { get; set; }
    public long BillItemId { get; set; }
    public BillItem? BillItem { get; set; }
    public decimal Amount { get; set; }
}
