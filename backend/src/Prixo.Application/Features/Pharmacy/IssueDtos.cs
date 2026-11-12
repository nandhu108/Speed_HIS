using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Pharmacy;

public record CreateIssueItemRequest(long BatchId, decimal Quantity);
public record CreateIssueRequest(
    long StoreId, IssueType IssueType, long? DepartmentId, long? PatientId, long? AdmissionId, long? IndentId, string? Notes,
    IReadOnlyList<CreateIssueItemRequest> Items);

public record IssueItemDto(long Id, long BatchId, string ProductName, string BatchNumber, decimal Quantity);

public record IssueListItemDto(
    long Id, string IssueNumber, DateOnly IssueDate, IssueType IssueType, string? DepartmentName, long? AdmissionId,
    string? AdmissionNumber, long? IndentId, int ItemCount);

public record IssueDetailDto(
    long Id, string IssueNumber, DateOnly IssueDate, IssueType IssueType, long? DepartmentId, string? DepartmentName,
    long? PatientId, string? PatientName, long? AdmissionId, string? AdmissionNumber, long? IndentId, string? Notes,
    IReadOnlyList<IssueItemDto> Items);

public record CreateIssueReturnItemRequest(long IssueItemId, decimal Quantity);
public record CreateIssueReturnRequest(long IssueId, string Reason, IReadOnlyList<CreateIssueReturnItemRequest> Items);
public record IssueReturnListItemDto(long Id, string ReturnNumber, DateOnly ReturnDate, string IssueNumber);
