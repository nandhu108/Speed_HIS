using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Quality;

public record RaiseNonConformanceRequest(
    long? StandardId, long? DepartmentId, string Title, string Description, NonConformanceSeverity Severity);

public record CloseNonConformanceRequest(string? Notes);

public record NonConformanceDto(
    long Id, long? StandardId, string? StandardCode, long? DepartmentId, string? DepartmentName, string Title,
    string Description, NonConformanceSeverity Severity, string RaisedByName, NonConformanceStatus Status,
    DateTimeOffset? ClosedAt, string? ClosedByName);

public record NonConformanceQuery(NonConformanceStatus? Status, int Page = 1, int PageSize = 20);
