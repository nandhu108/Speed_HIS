using Prixo.Application.Common.Models;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Pharmacy;

public record BatchAvailabilityDto(long BatchId, string BatchNumber, DateOnly ExpiryDate, decimal Mrp, decimal Available);

public record SaleItemDto(
    long Id, long BatchId, string DrugName, string BatchNumber, DateOnly ExpiryDate, decimal Quantity, decimal Rate,
    decimal DiscountPercent, decimal DiscountAmount, decimal TaxPercent, decimal TaxAmount, decimal TotalAmount);

/// <summary>DrugId + Quantity — the service resolves the actual batch(es) via FEFO; the caller doesn't pick a batch directly.</summary>
public record SaleLineRequest(long DrugId, decimal Quantity, decimal DiscountPercent);

public record CreateSaleRequest(
    long StoreId, long? PatientId, long? PrescriptionId, long? PaymentModeId, IReadOnlyList<SaleLineRequest> Items);

public record PharmacySaleListItemDto(
    long Id, string SaleNumber, DateOnly SaleDate, string? PatientName, PharmacySaleStatus Status, decimal TotalAmount);

public record PharmacySaleDetailDto(
    long Id, string SaleNumber, DateOnly SaleDate, long StoreId, string StoreName, long? PatientId, string? PatientName,
    PharmacySaleStatus Status, decimal SubTotal, decimal DiscountAmount, decimal TaxAmount, decimal TotalAmount,
    long? PaymentModeId, string? PaymentModeName,
    string? CancelReason, IReadOnlyList<SaleItemDto> Items);

public record CancelSaleRequest(string Reason);

/// <summary>"Pharmacy Pay Type Modify" — corrects the payment mode recorded against a completed
/// sale (e.g. the cashier picked the wrong mode at the POS, or the POS flow left it unset).</summary>
public record ModifyPayTypeRequest(long? PaymentModeId);

public class PharmacySaleQuery : PagedQuery
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public PharmacySaleStatus? Status { get; set; }
}

public record CreateSalesReturnItemRequest(long PharmacySaleItemId, decimal Quantity);
public record CreateSalesReturnRequest(long PharmacySaleId, string Reason, IReadOnlyList<CreateSalesReturnItemRequest> Items);
public record SalesReturnListItemDto(long Id, string ReturnNumber, DateOnly ReturnDate, string SaleNumber, decimal TotalAmount);
