using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Opd;

/// <summary>
/// Polymorphic order header (Lab/Radiology/Procedure). No dedicated test master or
/// downstream worklist exists yet (Phase 7/12) — Description is free text until then.
/// </summary>
public class ClinicalOrder : BaseEntity
{
    public long VisitId { get; set; }
    public OpVisit? Visit { get; set; }

    public ClinicalOrderType OrderType { get; set; }
    public required string Description { get; set; }
    public ClinicalOrderPriority Priority { get; set; } = ClinicalOrderPriority.Routine;
    public ClinicalOrderStatus Status { get; set; } = ClinicalOrderStatus.Ordered;
    public string? Notes { get; set; }
}
