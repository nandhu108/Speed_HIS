namespace Prixo.Domain.Enums;

/// <summary>Type of clinical encounter linked as an ABDM Care Context (Milestone M2 - HIP).</summary>
public enum AbdmCareContextType
{
    OpdVisit = 1,
    IpAdmission = 2,
    DiagnosticReport = 3,
    Prescription = 4,
    DischargeSummary = 5,
}
