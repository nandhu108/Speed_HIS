using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Admin;

/// <summary>Which branches a user may sign in to.</summary>
public class UserBranch : BaseEntity
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public long BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
}
