using Prixo.Application.Common.Models;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Lab;

public record LabOrderItemDto(
    long Id, long LabTestId, string LabTestName, long? TestProfileId, string? TestProfileName,
    long? SampleId, string? SampleNumber, LabOrderItemStatus Status);

public record LabOrderListItemDto(
    long Id, string OrderNumber, DateOnly OrderDate, long PatientId, string PatientUhid, string PatientName,
    ClinicalOrderPriority Priority, LabOrderStatus Status, int ItemCount);

public record LabOrderDetailDto(
    long Id, string OrderNumber, DateOnly OrderDate, long PatientId, string PatientUhid, string PatientName,
    string PatientGender, string PatientAgeDisplay, long? OpVisitId, long? EmergencyVisitId, long? OrderedByDoctorId,
    string? OrderedByDoctorName, ClinicalOrderPriority Priority, LabOrderStatus Status, string? ClinicalNotes,
    string? CancelReason, DateTimeOffset? CancelledAt, IReadOnlyList<LabOrderItemDto> Items);

public record CreateLabOrderItemRequest(long? LabTestId, long? TestProfileId);

public record CreateLabOrderRequest(
    long PatientId, long? OpVisitId, long? EmergencyVisitId, long? ClinicalOrderId, long? OrderedByDoctorId,
    ClinicalOrderPriority Priority, string? ClinicalNotes, IReadOnlyList<CreateLabOrderItemRequest> Items,
    long? AdmissionId = null);

public record CancelLabOrderRequest(string Reason);

public class LabOrderQuery : PagedQuery
{
    public long? PatientId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public LabOrderStatus? Status { get; set; }
}

// ── Collection / worklist ────────────────────────────────────────────────
public record CollectionWorklistItemDto(
    long LabOrderItemId, long LabOrderId, string OrderNumber, long PatientId, string PatientUhid,
    string PatientName, long LabTestId, string LabTestName, long SampleTypeId, string SampleTypeName,
    ClinicalOrderPriority Priority, DateOnly OrderDate);

public record CollectSampleRequest(long LabOrderId, long SampleTypeId, IReadOnlyList<long> LabOrderItemIds);

public record SampleDto(
    long Id, long LabOrderId, string OrderNumber, long SampleTypeId, string SampleTypeName,
    string SampleNumber, SampleStatus Status, DateTimeOffset CollectedAt, DateTimeOffset? ReceivedAt,
    string? RejectionReason, DateTimeOffset? RejectedAt);

public record RejectSampleRequest(string Reason);

public record DepartmentWorklistItemDto(
    long LabOrderItemId, long LabOrderId, string OrderNumber, long PatientId, string PatientUhid,
    string PatientName, string PatientGender, string PatientAgeDisplay, long LabTestId, string LabTestName,
    long? DepartmentId, string? DepartmentName, ClinicalOrderPriority Priority, DateOnly OrderDate);
