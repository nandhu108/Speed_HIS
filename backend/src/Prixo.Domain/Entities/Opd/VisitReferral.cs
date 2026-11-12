using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Opd;

public class VisitReferral : BaseEntity
{
    public long VisitId { get; set; }
    public OpVisit? Visit { get; set; }

    public ReferralDirection Direction { get; set; }
    /// <summary>Set when Direction is Internal.</summary>
    public long? ReferredToDoctorId { get; set; }
    public Doctor? ReferredToDoctor { get; set; }
    /// <summary>Set when Direction is External (clinic/hospital/specialist name).</summary>
    public string? ExternalDetails { get; set; }
    public required string Reason { get; set; }

    /// <summary>"Doctor Opinion Entry" — the referred-to doctor's cross-consult response. Only
    /// meaningful when Direction is Internal; a null OpinionEnteredAt means still pending.</summary>
    public string? OpinionNotes { get; set; }
    public DateTimeOffset? OpinionEnteredAt { get; set; }
}
