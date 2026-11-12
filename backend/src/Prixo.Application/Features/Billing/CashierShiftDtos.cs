using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Billing;

public record CashierShiftModeAmountDto(
    long PaymentModeId, string PaymentModeName, decimal SystemAmount, decimal DeclaredAmount, decimal Variance);

public record CashierShiftDto(
    long Id, long UserId, string UserName, DateTimeOffset OpenedAt, decimal OpeningFloat, CashierShiftStatus Status,
    DateTimeOffset? ClosedAt, string? Notes, long? ReceivedByUserId, string? ReceivedByName, DateTimeOffset? ReceivedAt,
    IReadOnlyList<CashierShiftModeAmountDto> ModeAmounts);

public record OpenCashierShiftRequest(decimal OpeningFloat);

public record CloseCashierShiftRequest(IReadOnlyList<DeclaredModeAmountRequest> DeclaredAmounts, string? Notes);

public record CashierShiftQuery(CashierShiftStatus? Status = null, int Page = 1, int PageSize = 20);
