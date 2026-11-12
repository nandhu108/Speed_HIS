using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Ipd;

public class ProgressNote : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public long DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    public DateTimeOffset NoteAt { get; set; }
    public required string Notes { get; set; }
}
