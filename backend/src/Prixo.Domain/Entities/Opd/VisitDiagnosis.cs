using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Opd;

/// <summary>
/// A diagnosis recorded at a visit. There is no separate "problem list" table — querying
/// a patient's diagnoses across visits (most recent per ICD code) serves as one.
/// </summary>
public class VisitDiagnosis : BaseEntity
{
    public long VisitId { get; set; }
    public OpVisit? Visit { get; set; }
    public long IcdCodeId { get; set; }
    public IcdCode? IcdCode { get; set; }

    public DiagnosisType Type { get; set; } = DiagnosisType.Provisional;
    public string? Notes { get; set; }
}
