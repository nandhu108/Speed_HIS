using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Icu;

// ── Ventilator ───────────────────────────────────────────────────────────
public record StartVentilatorSessionRequest(string? Notes);
public record AddVentilatorObservationRequest(
    VentilatorMode Mode, int FiO2Percent, int Peep, int? TidalVolumeMl, int? RespiratoryRateSet,
    decimal? PeakPressure, decimal? PlateauPressure, string? Notes);

public record VentilatorObservationDto(
    long Id, DateTimeOffset RecordedAt, string RecordedByName, VentilatorMode Mode, int FiO2Percent, int Peep,
    int? TidalVolumeMl, int? RespiratoryRateSet, decimal? PeakPressure, decimal? PlateauPressure, string? Notes);

public record VentilatorSessionDto(
    long Id, long AdmissionId, DateTimeOffset StartedAt, string StartedByName, DateTimeOffset? StoppedAt,
    string? Notes, IReadOnlyList<VentilatorObservationDto> Observations);

// ── Infusion ─────────────────────────────────────────────────────────────
public record StartInfusionRequest(long? DrugId, string? DrugNameOverride, string? Notes);
public record AddInfusionRateRequest(decimal Rate, string RateUnit, string? Notes);

public record InfusionRateEntryDto(long Id, DateTimeOffset RecordedAt, string RecordedByName, decimal Rate, string RateUnit, string? Notes);

public record InfusionChartEntryDto(
    long Id, long AdmissionId, long? DrugId, string ProductName, DateTimeOffset StartedAt, string StartedByName,
    DateTimeOffset? StoppedAt, string? Notes, IReadOnlyList<InfusionRateEntryDto> RateEntries);

// ── Severity ─────────────────────────────────────────────────────────────
public record RecordSeverityRequest(IcuSeverityLevel SeverityLevel, string? Notes);
public record IcuSeverityAssessmentDto(long Id, long AdmissionId, DateTimeOffset AssessedAt, string AssessedByName, IcuSeverityLevel SeverityLevel, string? Notes);
