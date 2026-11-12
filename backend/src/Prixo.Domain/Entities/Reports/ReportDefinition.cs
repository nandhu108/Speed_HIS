using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Reports;

/// <summary>
/// Custom Report Builder v1 catalog row (plan §17/§23: "report_definitions table: code, name,
/// parameters, permission, SQL view/proc, output formats"). Global, not branch-scoped — the
/// catalog itself is system-wide; branch scoping happens per-execution from the caller's own
/// session. Two shapes share this one table: a "linked" catalog entry (ViewName null,
/// LinkPath set) just points the report picker at an existing dedicated report page built in
/// an earlier phase (Lab TAT, Finance GST, etc.) — nothing to execute, no duplicate query.
/// An "executable" entry (ViewName set) is a real Custom Report Builder report: the generic
/// executor queries that `reporting` schema view/materialized view directly.
/// </summary>
public class ReportDefinition : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Category { get; set; }
    public required string PermissionCode { get; set; }
    public string? ViewName { get; set; }
    public string? LinkPath { get; set; }
    /// <summary>jsonb: [{"key": "departmentName", "label": "Department"}, ...] — drives the column picker and export headers.</summary>
    public required string ColumnsJson { get; set; }
}
