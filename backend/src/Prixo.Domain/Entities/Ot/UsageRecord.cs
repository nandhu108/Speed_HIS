using Prixo.Domain.Common;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Pharmacy;

namespace Prixo.Domain.Entities.Ot;

/// <summary>
/// Batch/serial-tracked implant used in a surgery — recording one both decrements the
/// batch's stock (an IssueOut StockLedger row, this usage row as the reference document)
/// and posts a BillItem to the admission's open IP bill, in one mandatory transaction
/// (OtUsageService). Rate is snapshotted from the batch's PurchaseRate at usage time, same
/// "Description/Rate snapshot, not just a live lookup" posture as every other BillItem.
/// </summary>
public class ImplantUsage : BaseEntity
{
    public long BookingId { get; set; }
    public SurgeryBooking? Booking { get; set; }
    public long BatchId { get; set; }
    public Batch? Batch { get; set; }
    public string? SerialNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public long? BillItemId { get; set; }
    public BillItem? BillItem { get; set; }
}

/// <summary>General consumable used in a surgery — same stock-decrement + billing posting as ImplantUsage, without serial tracking.</summary>
public class ConsumableUsage : BaseEntity
{
    public long BookingId { get; set; }
    public SurgeryBooking? Booking { get; set; }
    public long BatchId { get; set; }
    public Batch? Batch { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public long? BillItemId { get; set; }
    public BillItem? BillItem { get; set; }
}
