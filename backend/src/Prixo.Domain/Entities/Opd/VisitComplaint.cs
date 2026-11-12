using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Opd;

public class VisitComplaint : BaseEntity
{
    public long VisitId { get; set; }
    public OpVisit? Visit { get; set; }

    public required string Complaint { get; set; }
    /// <summary>Free text — e.g. "3 days", "2 weeks".</summary>
    public string? Duration { get; set; }
}
