using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Finance;

public record CreatePayableRequest(long GrnId, DateOnly? DueDate);
public record RecordPayablePaymentRequest(decimal Amount, long CashAccountId, DateOnly Date, string? ReferenceNumber);

public record PendingGrnDto(long GrnId, string GrnNumber, long VendorId, string VendorName, DateOnly ReceivedDate, string? SupplierInvoiceNumber, decimal TotalAmount);

public record PayableDto(
    long Id, long VendorId, string VendorName, long GrnId, string GrnNumber, string InvoiceNumber, DateOnly InvoiceDate,
    decimal InvoiceAmount, decimal PaidAmount, decimal BalanceAmount, DateOnly? DueDate, PayableStatus Status);

public record AgingRowDto(string EntityName, string Bucket, decimal BalanceAmount);

public class PayableQuery
{
    public PayableStatus? Status { get; set; }
}
