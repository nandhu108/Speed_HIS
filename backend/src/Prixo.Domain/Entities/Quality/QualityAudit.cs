using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Quality;

/// <summary>A scheduled/conducted run of a QualityAuditTemplate against a branch (plan §20: "scheduled audits, scoring").</summary>
public class QualityAudit : BranchEntity
{
    public long TemplateId { get; set; }
    public QualityAuditTemplate? Template { get; set; }

    public DateOnly ScheduledDate { get; set; }
    public DateOnly? ConductedDate { get; set; }
    public QualityAuditStatus Status { get; set; } = QualityAuditStatus.Scheduled;

    public long? ConductedByUserId { get; set; }
    public User? ConductedByUser { get; set; }
    public string? Remarks { get; set; }

    public List<QualityAuditResponse> Responses { get; set; } = [];
}

public class QualityAuditResponse : BaseEntity
{
    public long AuditId { get; set; }
    public QualityAudit? Audit { get; set; }

    public long TemplateItemId { get; set; }
    public QualityAuditTemplateItem? TemplateItem { get; set; }

    public decimal Score { get; set; }
    public string? Remarks { get; set; }
}
