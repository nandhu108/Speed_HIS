namespace Prixo.Application.Features.Er;

public record SaveMlcRequest(
    string? PoliceStationName, DateTimeOffset? PoliceInformedAt, string? FirNumber,
    string? BroughtByName, string? BroughtByDesignation, string? InjuryDescription, string? Notes);

public record MlcRecordDto(
    long Id, long EmergencyVisitId, string MlcNumber, string? PoliceStationName, DateTimeOffset? PoliceInformedAt,
    string? FirNumber, string? BroughtByName, string? BroughtByDesignation, string? InjuryDescription, string? Notes, string RecordedByName);

public record SaveAccidentRequest(string AccidentType, DateTimeOffset? AccidentAt, string? AccidentLocation, bool VehicleInvolved, string? Notes);

public record AccidentCaseRecordDto(
    long Id, long EmergencyVisitId, string AccidentType, DateTimeOffset? AccidentAt, string? AccidentLocation,
    bool VehicleInvolved, string? Notes, string RecordedByName);
