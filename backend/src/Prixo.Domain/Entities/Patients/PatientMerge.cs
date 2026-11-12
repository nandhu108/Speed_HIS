using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Patients;

/// <summary>
/// Audit record of a patient merge: MergedPatientId's data was re-pointed to
/// SurvivorPatientId in a single transaction and the merged record soft-deleted.
/// </summary>
public class PatientMerge : BaseEntity
{
    public long SurvivorPatientId { get; set; }
    public Patient? SurvivorPatient { get; set; }
    public long MergedPatientId { get; set; }
    public Patient? MergedPatient { get; set; }
    public required string Reason { get; set; }
}
