using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Lab;

public record EnterResultValueRequest(long LabTestParameterId, string Value);

public record EnterResultRequest(IReadOnlyList<EnterResultValueRequest> Values, string? Notes);

public record ResultValueDto(
    long Id, long LabTestParameterId, string ParameterName, string Value, string? Unit,
    decimal? NormalLow, decimal? NormalHigh, ResultFlag Flag);

public record LabResultDto(
    long Id, long LabOrderItemId, long EnteredByUserId, string? EnteredByName, DateTimeOffset EnteredAt,
    long? VerifiedByUserId, string? VerifiedByName, DateTimeOffset? VerifiedAt,
    long? ApprovedByUserId, string? ApprovedByName, DateTimeOffset? ApprovedAt,
    string? Notes, bool HasCriticalValue, IReadOnlyList<ResultValueDto> Values);

public record CriticalAlertDto(
    long Id, long LabResultId, long LabOrderItemId, string OrderNumber, string PatientName,
    long ResultValueId, string ParameterName, string Value, DateTimeOffset RaisedAt,
    bool IsAcknowledged, long? AcknowledgedByUserId, DateTimeOffset? AcknowledgedAt, string? AcknowledgementNotes);

public record AcknowledgeAlertRequest(string? Notes);

public record TatReportRowDto(
    long LabOrderItemId, string OrderNumber, string LabTestName, DateOnly OrderDate,
    DateTimeOffset? CollectedAt, DateTimeOffset? ReceivedAt, DateTimeOffset? ApprovedAt, double? TurnaroundHours);

/// <summary>
/// A result has been entered (or verified/approved) on an item that still has no
/// <see cref="Domain.Entities.Lab.LabOrderItem.BillItemId"/> — see "Unbilled Orders Report" in
/// docs/modules/lab.md. Advisory only, closing docs/PATIENT_JOURNEY_GAP_ANALYSIS.md §4.3:
/// nothing blocks collection/entry/approval on this link being null, this report just makes
/// the gap visible instead of silent.
/// </summary>
public record UnbilledLabOrderRowDto(
    long LabOrderItemId, string OrderNumber, DateOnly OrderDate, string PatientUhid, string PatientName,
    string LabTestName, LabOrderItemStatus Status);
