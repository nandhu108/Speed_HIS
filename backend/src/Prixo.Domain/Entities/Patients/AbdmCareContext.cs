using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Patients;

/// <summary>
/// A care context linked to ABDM under Milestone M2 (Health Information Provider - HIP).
/// Groups clinical encounters (OP visits, IP admissions, diagnostic reports, prescriptions)
/// and associates them with a patient's ABHA so they can be discovered and shared upon patient consent.
/// </summary>
public class AbdmCareContext : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    /// <summary>UHID of the patient at this facility (ABDM patientReference).</summary>
    public required string PatientReference { get; set; }

    /// <summary>14-digit ABHA number of the patient.</summary>
    public required string AbhaNumber { get; set; }

    /// <summary>The type of care encounter.</summary>
    public AbdmCareContextType ContextType { get; set; }

    /// <summary>Unique encounter reference inside this facility (e.g. OP visit code, IP number, order number).</summary>
    public required string ReferenceNumber { get; set; }

    /// <summary>Human-readable display title (e.g. "OP Consultation - General Medicine - 26 Oct 2026").</summary>
    public required string Display { get; set; }

    /// <summary>Whether this care context has been registered/linked with ABDM.</summary>
    public bool IsLinked { get; set; } = true;

    /// <summary>When the care context was linked with ABDM.</summary>
    public DateTimeOffset? LinkedAt { get; set; }
}
