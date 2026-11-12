using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Admin;

public class RolePresetRole : BaseEntity
{
    public long RolePresetId { get; set; }
    public RolePreset RolePreset { get; set; } = null!;
    public long RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
