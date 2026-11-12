using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Nursing;

/// <summary>Shift-tagged nursing notes — deepens IPD's plain ProgressNote (doctor-only) with a nurse/shift lens.</summary>
public class NursingNote : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public NursingShift Shift { get; set; }
    public DateTimeOffset NoteAt { get; set; }
    public long RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }

    public required string Notes { get; set; }
}
