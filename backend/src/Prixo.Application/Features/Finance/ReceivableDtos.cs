using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Finance;

public record CreateReceivableRequest(long BillId);
public record RecordReceivableCollectionRequest(decimal Amount, long CashAccountId, DateOnly Date, string? ReferenceNumber);

public record PendingCreditBillDto(long BillId, string BillNumber, long CorporateId, string CorporateName, DateOnly BillDate, decimal TotalAmount);

public record ReceivableDto(
    long Id, long CorporateId, string CorporateName, long BillId, string BillNumber, DateOnly BillDate,
    decimal BillAmount, decimal CollectedAmount, decimal BalanceAmount, ReceivableStatus Status);

public class ReceivableQuery
{
    public ReceivableStatus? Status { get; set; }
}
