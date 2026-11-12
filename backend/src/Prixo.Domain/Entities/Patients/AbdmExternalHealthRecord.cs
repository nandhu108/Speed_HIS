using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Patients;

/// <summary>
/// An external health record fetched from other healthcare facilities via ABDM (Milestone M3 - HIU).
/// Encrypted FHIR bundles from network HIPs are decrypted and parsed into this entity to provide
/// doctors with longitudinal medical history.
/// </summary>
public class AbdmExternalHealthRecord : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    public long? ConsentRequestId { get; set; }
    public AbdmConsentRequest? ConsentRequest { get; set; }

    /// <summary>Health Information type (e.g. OPConsultation, Prescription, DiagnosticReport, DischargeSummary).</summary>
    public AbdmHiType HiType { get; set; }

    /// <summary>Name of the source healthcare provider / hospital (e.g. "AIIMS New Delhi").</summary>
    public required string SourceFacilityName { get; set; }

    /// <summary>ABDM HIP ID of the source facility.</summary>
    public required string SourceFacilityHipId { get; set; }

    /// <summary>Clinical encounter date of the external record.</summary>
    public DateTimeOffset RecordDate { get; set; }

    /// <summary>Authoring practitioner at the external facility.</summary>
    public string? DoctorName { get; set; }

    /// <summary>Title or description of the record.</summary>
    public required string Title { get; set; }

    /// <summary>Extracted human-readable clinical summary (diagnoses, medications, lab values, notes).</summary>
    public required string ClinicalSummary { get; set; }

    /// <summary>Full NRCeS FHIR R4 JSON document bundle.</summary>
    public required string FhirBundleJson { get; set; }
}
