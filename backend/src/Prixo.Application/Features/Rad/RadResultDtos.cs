using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Rad;

public record EnterReportRequest(long? TemplateId, string Findings, string Impression);

public record RadReportDto(
    long Id, long RadOrderItemId, long? TemplateId, string Findings, string Impression,
    long EnteredByUserId, string? EnteredByName, DateTimeOffset EnteredAt,
    long? VerifiedByUserId, string? VerifiedByName, DateTimeOffset? VerifiedAt,
    long? ApprovedByUserId, string? ApprovedByName, DateTimeOffset? ApprovedAt);

/// <summary>Same shape for the entry/verify/approve queues, filtered by pipeline stage — mirrors Lab's DepartmentWorklistItemDto (no department split here, radiology has no department master link).</summary>
public record ReportingWorklistItemDto(
    long RadOrderItemId, long RadOrderId, string OrderNumber, long PatientId, string PatientUhid, string PatientName,
    string PatientGender, string PatientAgeDisplay, long RadiologyTestId, string RadiologyTestName,
    ClinicalOrderPriority Priority, DateOnly OrderDate);
