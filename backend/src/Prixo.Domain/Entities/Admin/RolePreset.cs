using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Admin;

/// <summary>
/// A named, reusable combination of roles an admin can apply in one click when
/// creating/editing a user — a personal shortcut, not itself a security boundary
/// (the roles it references still carry the real permissions).
/// </summary>
public class RolePreset : BaseEntity
{
    public required string Name { get; set; }
    public ICollection<RolePresetRole> RolePresetRoles { get; set; } = [];
}
