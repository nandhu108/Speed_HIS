using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Admin;

public class SystemSetting : BaseEntity
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public string? Description { get; set; }
    /// <summary>Grouping for the settings UI: Security, Password, Session, Backup, Notification…</summary>
    public string Group { get; set; } = "General";
}
