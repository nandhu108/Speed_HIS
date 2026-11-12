using Prixo.Application.Common.Models;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Pharmacy;

// ── Purchase requests ────────────────────────────────────────────────────
public record PurchaseRequestItemDto(long Id, long? DrugId, long? ItemId, string ProductName, decimal RequestedQuantity);
public record CreatePurchaseRequestItemRequest(long? DrugId, long? ItemId, decimal RequestedQuantity);
public record CreatePurchaseRequestRequest(string? Notes, IReadOnlyList<CreatePurchaseRequestItemRequest> Items);
public record DecidePurchaseRequestRequest(bool Approve);

public record PurchaseRequestListItemDto(
    long Id, string RequestNumber, DateOnly RequestDate, PurchaseRequestStatus Status, int ItemCount);

public record PurchaseRequestDetailDto(
    long Id, string RequestNumber, DateOnly RequestDate, PurchaseRequestStatus Status, string? Notes,
    IReadOnlyList<PurchaseRequestItemDto> Items);

public class PurchaseRequestQuery : PagedQuery
{
    public PurchaseRequestStatus? Status { get; set; }
}

// ── Purchase orders ──────────────────────────────────────────────────────
public record PurchaseOrderItemDto(
    long Id, long? DrugId, long? ItemId, string ProductName, decimal OrderedQuantity, decimal ReceivedQuantity, decimal Rate, decimal TaxPercent);
public record CreatePurchaseOrderItemRequest(long? DrugId, long? ItemId, decimal OrderedQuantity, decimal Rate, decimal TaxPercent);
public record CreatePurchaseOrderRequest(
    long VendorId, long? PurchaseRequestId, string? Notes, IReadOnlyList<CreatePurchaseOrderItemRequest> Items);

public record PurchaseOrderListItemDto(
    long Id, string PoNumber, DateOnly OrderDate, long VendorId, string VendorName, PurchaseOrderStatus Status, decimal TotalAmount);

public record PurchaseOrderDetailDto(
    long Id, string PoNumber, DateOnly OrderDate, long VendorId, string VendorName, long? PurchaseRequestId,
    PurchaseOrderStatus Status, string? Notes, IReadOnlyList<PurchaseOrderItemDto> Items);

public class PurchaseOrderQuery : PagedQuery
{
    public PurchaseOrderStatus? Status { get; set; }
}

// ── GRN ──────────────────────────────────────────────────────────────────
public record GrnItemDto(
    long Id, long? DrugId, long? ItemId, string ProductName, string BatchNumber, DateOnly? ExpiryDate, decimal? Mrp,
    decimal PurchaseRate, decimal ReceivedQuantity, decimal FreeQuantity, decimal TaxPercent, decimal Amount);

public record CreateGrnItemRequest(
    long? DrugId, long? ItemId, string BatchNumber, DateOnly? ExpiryDate, decimal? Mrp, decimal PurchaseRate,
    decimal ReceivedQuantity, decimal FreeQuantity, decimal TaxPercent);

public record CreateGrnRequest(
    long? PurchaseOrderId, long VendorId, long StoreId, string? SupplierInvoiceNumber, DateOnly? SupplierInvoiceDate,
    IReadOnlyList<CreateGrnItemRequest> Items);

public record GrnListItemDto(
    long Id, string GrnNumber, DateOnly ReceivedDate, long VendorId, string VendorName, DocumentStatus Status, decimal TotalAmount);

public record GrnDetailDto(
    long Id, string GrnNumber, DateOnly ReceivedDate, long? PurchaseOrderId, long VendorId, string VendorName,
    long StoreId, string StoreName, string? SupplierInvoiceNumber, DateOnly? SupplierInvoiceDate,
    DocumentStatus Status, decimal TotalAmount, IReadOnlyList<GrnItemDto> Items);

public class GrnQuery : PagedQuery
{
    public DocumentStatus? Status { get; set; }
}

// ── Purchase returns ─────────────────────────────────────────────────────
public record PurchaseReturnItemDto(long Id, long BatchId, string ProductName, string BatchNumber, decimal Quantity, decimal Rate);
public record CreatePurchaseReturnItemRequest(long BatchId, decimal Quantity, decimal Rate);
public record CreatePurchaseReturnRequest(
    long? GrnId, long VendorId, string Reason, IReadOnlyList<CreatePurchaseReturnItemRequest> Items);

public record PurchaseReturnListItemDto(
    long Id, string ReturnNumber, DateOnly ReturnDate, long VendorId, string VendorName, decimal TotalAmount);

public record PurchaseReturnDetailDto(
    long Id, string ReturnNumber, DateOnly ReturnDate, long? GrnId, long VendorId, string VendorName, string Reason,
    decimal TotalAmount, IReadOnlyList<PurchaseReturnItemDto> Items);
