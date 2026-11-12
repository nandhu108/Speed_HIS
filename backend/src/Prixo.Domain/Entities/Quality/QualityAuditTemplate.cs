using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Quality;

/// <summary>Audit Checklist template (plan §20: "Audit Checklist (templates, scheduled audits, scoring)"). Global master, like a Lab test profile.</summary>
public class QualityAuditTemplate : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Category { get; set; }

    public List<QualityAuditTemplateItem> Items { get; set; } = [];
}

public class QualityAuditTemplateItem : BaseEntity
{
    public long TemplateId { get; set; }
    public QualityAuditTemplate? Template { get; set; }

    public int SortOrder { get; set; }
    public required string Question { get; set; }
    public decimal MaxScore { get; set; } = 5;
}
