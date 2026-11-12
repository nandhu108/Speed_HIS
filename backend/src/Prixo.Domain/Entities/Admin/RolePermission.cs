using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Admin;

public class RolePermission : BaseEntity
{
    public long RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public long PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
