using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Admin;

/// <summary>
/// A per-user exception on top of the role-derived permission set: IsGranted=true adds a
/// permission the user's roles don't carry, IsGranted=false explicitly denies one they would
/// otherwise have. Resolved in <c>PermissionService.GetUserPermissionsAsync</c>.
/// </summary>
public class UserPermissionOverride : BaseEntity
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public long PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public bool IsGranted { get; set; }
}
