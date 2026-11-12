using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Pharmacy;

public class PurchaseRequest : BranchEntity
{
    public required string RequestNumber { get; set; }
    public DateOnly RequestDate { get; set; }
    public PurchaseRequestStatus Status { get; set; } = PurchaseRequestStatus.Draft;
    public string? Notes { get; set; }

    public ICollection<PurchaseRequestItem> Items { get; set; } = [];
}

/// <summary>Exactly one of DrugId/ItemId is set — see Batch's doc comment for the pattern.</summary>
public class PurchaseRequestItem : BaseEntity
{
    public long PurchaseRequestId { get; set; }
    public PurchaseRequest? PurchaseRequest { get; set; }
    public long? DrugId { get; set; }
    public Drug? Drug { get; set; }
    public long? ItemId { get; set; }
    public Item? Item { get; set; }
    public decimal RequestedQuantity { get; set; }
}

public class PurchaseOrder : BranchEntity
{
    public long VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public long? PurchaseRequestId { get; set; }
    public PurchaseRequest? PurchaseRequest { get; set; }
    public required string PoNumber { get; set; }
    public DateOnly OrderDate { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public string? Notes { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = [];
}

/// <summary>Exactly one of DrugId/ItemId is set — see Batch's doc comment for the pattern.</summary>
public class PurchaseOrderItem : BaseEntity
{
    public long PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public long? DrugId { get; set; }
    public Drug? Drug { get; set; }
    public long? ItemId { get; set; }
    public Item? Item { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal Rate { get; set; }
    public decimal TaxPercent { get; set; }
}

/// <summary>
/// Goods Receipt Note — posting one creates/tops-up a Batch per line and writes a
/// PurchaseIn StockLedger row, transactionally. Supplier-invoice fields are folded in
/// here rather than a separate SupplierInvoice entity — in this simple flow a GRN and its
/// invoice are 1:1, so a separate table would just be a redundant join.
/// </summary>
public class Grn : BranchEntity
{
    public long? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public long VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public long StoreId { get; set; }
    public Store? Store { get; set; }
    public required string GrnNumber { get; set; }
    public DateOnly ReceivedDate { get; set; }
    public string? SupplierInvoiceNumber { get; set; }
    public DateOnly? SupplierInvoiceDate { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public decimal TotalAmount { get; set; }

    public ICollection<GrnItem> Items { get; set; } = [];
}

/// <summary>Exactly one of DrugId/ItemId is set — see Batch's doc comment for the pattern.</summary>
public class GrnItem : BaseEntity
{
    public long GrnId { get; set; }
    public Grn? Grn { get; set; }
    public long? DrugId { get; set; }
    public Drug? Drug { get; set; }
    public long? ItemId { get; set; }
    public Item? Item { get; set; }
    public required string BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal? Mrp { get; set; }
    public decimal PurchaseRate { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal FreeQuantity { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal Amount { get; set; }
    /// <summary>Set once this line is posted — the batch it created or topped up.</summary>
    public long? BatchId { get; set; }
    public Batch? Batch { get; set; }
}

public class PurchaseReturn : BranchEntity
{
    public long? GrnId { get; set; }
    public Grn? Grn { get; set; }
    public long VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public required string ReturnNumber { get; set; }
    public DateOnly ReturnDate { get; set; }
    public required string Reason { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public decimal TotalAmount { get; set; }

    public ICollection<PurchaseReturnItem> Items { get; set; } = [];
}

public class PurchaseReturnItem : BaseEntity
{
    public long PurchaseReturnId { get; set; }
    public PurchaseReturn? PurchaseReturn { get; set; }
    public long BatchId { get; set; }
    public Batch? Batch { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
}
