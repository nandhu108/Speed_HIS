using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Icu;

public record IcuVitalsAlertDto(
    long Id, long AdmissionId, string PatientName, long IpVitalsId, CriticalAlertParameter Parameter,
    decimal Value, decimal ThresholdValue, ComparisonOperator Operator, AlertSeverity Severity,
    DateTimeOffset RaisedAt, bool IsAcknowledged, string? AcknowledgedByName, DateTimeOffset? AcknowledgedAt, string? AcknowledgementNotes);

public record AcknowledgeAlertRequest(string? Notes);
