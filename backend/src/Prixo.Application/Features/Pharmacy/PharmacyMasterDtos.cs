using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Pharmacy;

public record StoreDto(long Id, string Code, string Name, StoreType StoreType, bool IsDefault, bool IsActive);
public record SaveStoreRequest(string Code, string Name, StoreType StoreType, bool IsDefault, bool IsActive = true);

public record RackDto(long Id, long StoreId, string StoreName, string Code, string Name);
public record SaveRackRequest(long StoreId, string Code, string Name);

// ── Indents ───────────────────────────────────────────────────────────────
public record IndentItemDto(long Id, long? DrugId, long? ItemId, string ProductName, decimal RequestedQuantity, decimal? ApprovedQuantity);
public record CreateIndentItemRequest(long? DrugId, long? ItemId, decimal RequestedQuantity);
public record CreateIndentRequest(long StoreId, long DepartmentId, string? Notes, IReadOnlyList<CreateIndentItemRequest> Items);

public record DecideIndentItemRequest(long IndentItemId, decimal ApprovedQuantity);
public record DecideIndentRequest(bool Approve, string? RejectionReason, IReadOnlyList<DecideIndentItemRequest>? Items);

public record IndentListItemDto(
    long Id, string IndentNumber, DateOnly IndentDate, string StoreName, string DepartmentName, IndentStatus Status, int ItemCount);

public record IndentDetailDto(
    long Id, string IndentNumber, DateOnly IndentDate, long StoreId, string StoreName, long DepartmentId, string DepartmentName,
    IndentStatus Status, string? Notes, string? RejectionReason, IReadOnlyList<IndentItemDto> Items);

public class IndentQuery
{
    public IndentStatus? Status { get; set; }
}
