using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Emr;

public record AddMedicalHistoryRequest(string Condition, DateOnly? DiagnosedDate, string? Notes);
public record MedicalHistoryDto(long Id, string Condition, DateOnly? DiagnosedDate, string? Notes, string RecordedByName, DateTimeOffset RecordedAt);

public record AddSurgicalHistoryRequest(string ProcedureName, DateOnly? ProcedureDate, string? Surgeon, string? Notes);
public record SurgicalHistoryDto(long Id, string ProcedureName, DateOnly? ProcedureDate, string? Surgeon, string? Notes, string RecordedByName, DateTimeOffset RecordedAt);

public record AddFamilyHistoryRequest(string Relation, string Condition, string? Notes);
public record FamilyHistoryDto(long Id, string Relation, string Condition, string? Notes, string RecordedByName, DateTimeOffset RecordedAt);

public record AddImmunizationRequest(string VaccineName, int? DoseNumber, DateOnly AdministeredDate, DateOnly? NextDueDate, string? AdministeredBy, string? Notes);
public record ImmunizationDto(
    long Id, string VaccineName, int? DoseNumber, DateOnly AdministeredDate, DateOnly? NextDueDate,
    string? AdministeredBy, string? Notes, string RecordedByName, DateTimeOffset RecordedAt);

public record AddAllergyRequest(AllergenType AllergenType, string AllergenName, string? Reaction, AllergySeverity Severity);
public record AllergyDto(
    long Id, AllergenType AllergenType, string AllergenName, string? Reaction, AllergySeverity Severity,
    bool IsActive, string RecordedByName, DateTimeOffset RecordedAt);

public record AllergyMatchDto(string DrugName, long AllergyId, string AllergenName, string? Reaction, AllergySeverity Severity);
