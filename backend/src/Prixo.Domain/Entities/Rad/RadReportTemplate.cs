using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Rad;

/// <summary>Structured reporting template per test (plan §12 "structured templates + rich text") — canned Findings/Impression text a radiologist starts from and edits; "rich text" here is a plain long-text field, same posture as every other narrative field in this codebase.</summary>
public class RadReportTemplate : BaseEntity
{
    public long RadiologyTestId { get; set; }
    public RadiologyTest? RadiologyTest { get; set; }

    public required string Name { get; set; }
    public required string FindingsTemplate { get; set; }
    public required string ImpressionTemplate { get; set; }
}
