using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Ipd;

public class ProcedureNote : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public required string ProcedureName { get; set; }
    public long PerformedByDoctorId { get; set; }
    public Doctor? PerformedByDoctor { get; set; }
    public DateTimeOffset PerformedAt { get; set; }
    public string? Findings { get; set; }
    public string? Notes { get; set; }
}
