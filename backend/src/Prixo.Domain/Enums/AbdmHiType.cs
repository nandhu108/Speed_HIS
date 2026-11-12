namespace Prixo.Domain.Enums;

/// <summary>Health Information Types (HI Types) defined by ABDM / NRCeS for FHIR data exchange.</summary>
public enum AbdmHiType
{
    Prescription = 1,
    DiagnosticReport = 2,
    OpConsultation = 3,
    DischargeSummary = 4,
    ImmunizationRecord = 5,
    HealthDocumentRecord = 6,
}
