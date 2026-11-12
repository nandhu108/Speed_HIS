using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Billing;

public record DailyCollectionModeDto(long PaymentModeId, string PaymentModeName, decimal Amount, int Count);

public record DailyCollectionDto(DateOnly Date, IReadOnlyList<DailyCollectionModeDto> ByMode, decimal Total);

public record DayCloseModeAmountDto(
    long PaymentModeId, string PaymentModeName, decimal SystemAmount, decimal DeclaredAmount, decimal Variance);

public record DayCloseDto(
    long Id, DateOnly Date, DayCloseStatus Status, DateTimeOffset? ClosedAt, string? ClosedByName, string? Notes,
    IReadOnlyList<DayCloseModeAmountDto> ModeAmounts);

public record DeclaredModeAmountRequest(long PaymentModeId, decimal DeclaredAmount);

public record CloseDayRequest(DateOnly Date, IReadOnlyList<DeclaredModeAmountRequest> DeclaredAmounts, string? Notes);
