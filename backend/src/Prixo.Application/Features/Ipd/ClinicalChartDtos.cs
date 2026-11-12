namespace Prixo.Application.Features.Ipd;

public record AddProgressNoteRequest(long DoctorId, string Notes);
public record ProgressNoteDto(long Id, long DoctorId, string DoctorName, DateTimeOffset NoteAt, string Notes);

public record AddVitalsRequest(
    decimal? TemperatureF, int? PulseRate, int? RespiratoryRate, int? BloodPressureSystolic,
    int? BloodPressureDiastolic, int? SpO2, decimal? RandomBloodSugar,
    int? GlasgowComaScale, int? MeanArterialPressure, int? CentralVenousPressure, decimal? UrineOutputMl);
public record IpVitalsDto(
    long Id, DateTimeOffset RecordedAt, string RecordedByName, decimal? TemperatureF, int? PulseRate,
    int? RespiratoryRate, int? BloodPressureSystolic, int? BloodPressureDiastolic, int? SpO2, decimal? RandomBloodSugar,
    int? GlasgowComaScale, int? MeanArterialPressure, int? CentralVenousPressure, decimal? UrineOutputMl);

public record AddIntakeOutputRequest(bool IsIntake, string Category, decimal QuantityMl, string? Notes);
public record IntakeOutputDto(long Id, DateTimeOffset RecordedAt, bool IsIntake, string Category, decimal QuantityMl, string? Notes);

public record AddProcedureNoteRequest(string ProcedureName, long PerformedByDoctorId, DateTimeOffset PerformedAt, string? Findings, string? Notes);
public record ProcedureNoteDto(long Id, string ProcedureName, long PerformedByDoctorId, string PerformedByDoctorName, DateTimeOffset PerformedAt, string? Findings, string? Notes);
