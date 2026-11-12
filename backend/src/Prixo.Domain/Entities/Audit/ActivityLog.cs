namespace Prixo.Domain.Entities.Audit;

/// <summary>
/// Explicit business/security events (login, logout, failed login, password change,
/// permission change, force logout…). Append-only — deliberately NOT a BaseEntity.
/// </summary>
public class ActivityLog
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string? Username { get; set; }
    public long? BranchId { get; set; }
    public required string EventType { get; set; }
    public required string Description { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
