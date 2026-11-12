using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Mrd;

/// <summary>
/// ICD Coding worklist (plan §17: "coder worklist for discharged cases, coding completeness").
/// One row per Admission, created lazily the first time a coder opens/completes a case —
/// diagnosis codes themselves live in EMR/OPD/IPD's existing Diagnosis records; this just
/// tracks the MRD coder's review/completion of that admission's coding.
/// </summary>
public class CodingWorklistEntry : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public CodingStatus Status { get; set; } = CodingStatus.Pending;
    public long? CodedByUserId { get; set; }
    public User? CodedByUser { get; set; }
    public DateTimeOffset? CodedAt { get; set; }
    public string? Notes { get; set; }
}
