using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Admin;

/// <summary>Prevents reuse of recent passwords (policy: last N, default 5).</summary>
public class PasswordHistory : BaseEntity
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public required string PasswordHash { get; set; }
}
