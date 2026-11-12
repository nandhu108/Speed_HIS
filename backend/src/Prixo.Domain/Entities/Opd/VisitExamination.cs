using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Opd;

/// <summary>History taking + examination notes — one record per visit.</summary>
public class VisitExamination : BaseEntity
{
    public long VisitId { get; set; }
    public OpVisit? Visit { get; set; }

    public string? HistoryNotes { get; set; }
    public string? ExaminationNotes { get; set; }
}
