using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;

namespace Prixo.Domain.Entities.Insurance;

/// <summary>
/// An insurance/TPA policy held by a patient. A patient may hold more than one over time
/// (TPA changes, renewals) — same repeatable-collection shape as PatientContact/PatientIdentifier.
/// </summary>
public class PatientInsurancePolicy : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    public long InsuranceCompanyId { get; set; }
    public InsuranceCompany? InsuranceCompany { get; set; }

    public required string PolicyNumber { get; set; }
    public DateOnly? PolicyValidFrom { get; set; }
    public DateOnly? PolicyValidTo { get; set; }
    public bool IsPrimary { get; set; }
}
