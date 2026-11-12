namespace Prixo.Domain.Entities.Audit;

/// <summary>
/// Row-diff audit written automatically by the persistence interceptor in the same
/// transaction as the change. Append-only — deliberately NOT a BaseEntity.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long? BranchId { get; set; }
    public required string EntityName { get; set; }
    public required string EntityId { get; set; }
    public required string Action { get; set; }          // Created | Updated | Deleted
    public string? OldValues { get; set; }               // jsonb, changed columns only
    public string? NewValues { get; set; }               // jsonb
    public string? TraceId { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
