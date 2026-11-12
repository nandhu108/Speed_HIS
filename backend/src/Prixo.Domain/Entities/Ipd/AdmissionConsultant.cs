using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Ipd;

/// <summary>
/// One row per consultant mapping — both the initial primary+secondary mapping and every
/// consultant transfer (a transfer ends the current row's EndedAt and inserts a new one),
/// so full history reads off this one table without a separate "transfer" entity.
/// </summary>
public class AdmissionConsultant : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public long DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public ConsultantRole Role { get; set; } = ConsultantRole.Secondary;
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string? Notes { get; set; }
}
