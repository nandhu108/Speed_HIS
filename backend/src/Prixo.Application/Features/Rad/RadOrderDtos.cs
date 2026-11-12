using Prixo.Application.Common.Models;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Rad;

public record RadOrderItemDto(
    long Id, long RadiologyTestId, string RadiologyTestName, string ModalityName, string AccessionNumber,
    RadOrderItemStatus Status, bool RequiresContrast, bool HasContrastConsent, DateTimeOffset? ScheduledAt);

public record RadOrderListItemDto(
    long Id, string OrderNumber, DateOnly OrderDate, long PatientId, string PatientUhid, string PatientName,
    ClinicalOrderPriority Priority, RadOrderStatus Status, int ItemCount);

public record RadOrderDetailDto(
    long Id, string OrderNumber, DateOnly OrderDate, long PatientId, string PatientUhid, string PatientName,
    string PatientGender, string PatientAgeDisplay, long? OpVisitId, long? AdmissionId, long? EmergencyVisitId,
    long? OrderedByDoctorId, string? OrderedByDoctorName, ClinicalOrderPriority Priority, RadOrderStatus Status,
    string? ClinicalNotes, string? CancelReason, DateTimeOffset? CancelledAt, IReadOnlyList<RadOrderItemDto> Items);

public record CreateRadOrderRequest(
    long PatientId, long? OpVisitId, long? AdmissionId, long? EmergencyVisitId, long? ClinicalOrderId,
    long? OrderedByDoctorId, ClinicalOrderPriority Priority, string? ClinicalNotes, IReadOnlyList<long> RadiologyTestIds);

public record CancelRadOrderRequest(string Reason);

public class RadOrderQuery : PagedQuery
{
    public long? PatientId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public RadOrderStatus? Status { get; set; }
}

// ── Scheduling ───────────────────────────────────────────────────────────
public record SchedulingWorklistItemDto(
    long RadOrderItemId, long RadOrderId, string OrderNumber, long PatientId, string PatientUhid, string PatientName,
    long RadiologyTestId, string RadiologyTestName, long ModalityId, string ModalityName, bool RequiresContrast,
    ClinicalOrderPriority Priority, DateOnly OrderDate);

public record ScheduleItemRequest(DateTimeOffset ScheduledAt, string? Notes);

// ── Image capture ────────────────────────────────────────────────────────
public record CaptureWorklistItemDto(
    long RadOrderItemId, long RadOrderId, string OrderNumber, long PatientId, string PatientUhid, string PatientName,
    long RadiologyTestId, string RadiologyTestName, long ModalityId, string ModalityName, bool RequiresContrast,
    bool HasContrastConsent, DateTimeOffset ScheduledAt, ClinicalOrderPriority Priority);

// ── Contrast consent ─────────────────────────────────────────────────────
public record CaptureContrastConsentRequest(string SignedByName, string? RelationshipToPatient);

public record ContrastConsentDto(long Id, string SignedByName, string? RelationshipToPatient, DateTimeOffset SignedAt);

public record RadTatReportRowDto(
    long RadOrderItemId, string OrderNumber, string RadiologyTestName, DateOnly OrderDate,
    DateTimeOffset? ScheduledAt, DateTimeOffset? ImageCapturedAt, DateTimeOffset? ApprovedAt, double? TurnaroundHours);

/// <summary>Mirrors Lab's UnbilledLabOrderRowDto — see its doc comment.</summary>
public record UnbilledRadOrderRowDto(
    long RadOrderItemId, string OrderNumber, DateOnly OrderDate, string PatientUhid, string PatientName,
    string RadiologyTestName, RadOrderItemStatus Status);
