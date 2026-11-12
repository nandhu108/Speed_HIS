using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Nursing;

public record ReportIncidentRequest(
    DateTimeOffset IncidentDate, string IncidentType, string Location, IncidentSeverity Severity, string Description,
    string? ImmediateActionTaken, long? PatientId, long? AdmissionId);

public record ReviewIncidentRequest(string ReviewNotes);
public record CloseIncidentRequest(string? ReviewNotes);

/// <summary>Quality's RCA action (QUALITY.INCIDENT.RCA) — widens the existing incident record rather than a parallel Quality-side entity; see Incident's own doc comment.</summary>
public record RecordIncidentRcaRequest(string RootCauseAnalysis, bool IsSentinelEvent);

public record IncidentDto(
    long Id, DateTimeOffset IncidentDate, string IncidentType, string Location, IncidentSeverity Severity, string Description,
    string? ImmediateActionTaken, long? PatientId, string? PatientName, long? AdmissionId, string? AdmissionNumber,
    string ReportedByName, IncidentStatus Status, string? ReviewNotes, string? ReviewedByName, DateTimeOffset? ClosedAt,
    string? SourceModule, bool IsSentinelEvent, string? RootCauseAnalysis);

public record IncidentQuery(
    IncidentStatus? Status, IncidentSeverity? Severity, DateOnly? FromDate, DateOnly? ToDate,
    bool? SentinelOnly = null, int Page = 1, int PageSize = 20);
