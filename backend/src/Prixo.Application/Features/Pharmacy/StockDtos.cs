using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Pharmacy;

// ── Stock explorer ───────────────────────────────────────────────────────
public record BatchDto(
    long Id, long? DrugId, long? ItemId, string ProductName, long StoreId, string StoreName, string BatchNumber,
    DateOnly? ExpiryDate, decimal? Mrp, decimal PurchaseRate, decimal QuantityOnHand);

public record StockLedgerEntryDto(
    long Id, StockMovementType MovementType, decimal Quantity, decimal RunningBalance,
    string ReferenceType, long ReferenceId, DateTimeOffset MovementAt, string? Notes);

// ── Stock adjustment ─────────────────────────────────────────────────────
public record CreateStockAdjustmentItemRequest(long BatchId, decimal QuantityChange);
public record CreateStockAdjustmentRequest(long StoreId, string Reason, IReadOnlyList<CreateStockAdjustmentItemRequest> Items);
public record DecideStockAdjustmentRequest(bool Approve, string? RejectionReason);

public record StockAdjustmentItemDto(long Id, long BatchId, string ProductName, string BatchNumber, decimal QuantityChange);

public record StockAdjustmentListItemDto(
    long Id, string AdjustmentNumber, DateOnly AdjustmentDate, string StoreName, StockAdjustmentStatus Status);

public record StockAdjustmentDetailDto(
    long Id, string AdjustmentNumber, DateOnly AdjustmentDate, long StoreId, string StoreName, string Reason,
    StockAdjustmentStatus Status, string? RejectionReason, IReadOnlyList<StockAdjustmentItemDto> Items);

// ── Stock transfer ───────────────────────────────────────────────────────
public record CreateStockTransferItemRequest(long BatchId, decimal Quantity);
public record CreateStockTransferRequest(
    long FromStoreId, long ToBranchId, long ToStoreId, IReadOnlyList<CreateStockTransferItemRequest> Items);

public record StockTransferItemDto(long Id, long BatchId, string ProductName, string BatchNumber, decimal Quantity);

public record StockTransferListItemDto(
    long Id, string TransferNumber, DateOnly TransferDate, string FromStoreName, string ToStoreName, StockTransferStatus Status);

public record StockTransferDetailDto(
    long Id, string TransferNumber, DateOnly TransferDate, long FromStoreId, string FromStoreName,
    long ToBranchId, string ToBranchName, long ToStoreId, string ToStoreName, StockTransferStatus Status,
    IReadOnlyList<StockTransferItemDto> Items);

// ── Stock audit ───────────────────────────────────────────────────────────
public record CreateStockAuditRequest(long StoreId);
public record AddAuditCountRequest(long BatchId, decimal CountedQuantity);

public record StockAuditItemDto(long Id, long BatchId, string ProductName, string BatchNumber, decimal SystemQuantity, decimal CountedQuantity, decimal Variance);

public record StockAuditListItemDto(long Id, string AuditNumber, DateOnly AuditDate, string StoreName, StockAuditStatus Status);

public record StockAuditDetailDto(
    long Id, string AuditNumber, DateOnly AuditDate, long StoreId, string StoreName, StockAuditStatus Status,
    IReadOnlyList<StockAuditItemDto> Items);

// ── Alerts / reports ─────────────────────────────────────────────────────
public record NearExpiryBatchDto(long BatchId, string ProductName, string StoreName, string BatchNumber, DateOnly ExpiryDate, decimal QuantityOnHand, int DaysToExpiry);

public record LowStockProductDto(long? DrugId, long? ItemId, string ProductName, decimal ReorderLevel, decimal TotalOnHand);

/// <summary>DepartmentName is null for OP pharmacy sale movements (no department involved); populated for department/emergency/IP issues.</summary>
public record ProductConsumptionRowDto(long? DrugId, long? ItemId, string ProductName, string? DepartmentName, decimal QuantitySold, decimal QuantityIssued);
