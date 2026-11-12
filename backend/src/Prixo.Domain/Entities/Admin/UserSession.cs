using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Admin;

public class UserSession : BaseEntity
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid SessionId { get; set; }
    public long BranchId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset LoginAt { get; set; }
    public DateTimeOffset LastActivityAt { get; set; }
    public DateTimeOffset? LoggedOutAt { get; set; }
    public bool IsRevoked { get; set; }
}
