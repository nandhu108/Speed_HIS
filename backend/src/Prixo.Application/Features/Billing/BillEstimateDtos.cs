using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Billing;

public record BillEstimateListItemDto(
    long Id, string EstimateNumber, DateOnly EstimateDate, long PatientId, string PatientUhid, string PatientName,
    BillEstimateStatus Status, decimal TotalAmount);

public record BillEstimateItemDto(
    long Id, BillItemSourceType SourceType, long? ServiceId, string? ServiceName, long? PackageId,
    string? PackageName, string Description, decimal Quantity, decimal Rate, decimal TotalAmount);

public record BillEstimateDetailDto(
    long Id, string EstimateNumber, DateOnly EstimateDate, long PatientId, string PatientUhid, string PatientName,
    long? BillEstimateTemplateId, string? BillEstimateTemplateName, BillEstimateStatus Status, decimal TotalAmount,
    string? Notes, IReadOnlyList<BillEstimateItemDto> Items);

public record CreateEstimateItemRequest(BillItemSourceType SourceType, long? ServiceId, long? PackageId, decimal Quantity);

public record CreateBillEstimateRequest(
    long PatientId, long? BillEstimateTemplateId, IReadOnlyList<CreateEstimateItemRequest> Items, string? Notes);

public record BillEstimateQuery(long? PatientId, int Page = 1, int PageSize = 20);
