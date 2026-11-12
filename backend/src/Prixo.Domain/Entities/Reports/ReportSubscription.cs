namespace Prixo.Domain.Entities.Reports;

/// <summary>
/// Report subscription outbox placeholder (plan §23: "Report subscription (placeholder:
/// schedule + outbox)") — same posture as Appointments' NotificationQueue: real schema
/// captured now (who wants which report, how often, where to), but nothing ever dispatches
/// it until a mailer (held/Communication module territory) exists. Not a BaseEntity — infra
/// table, not a business/audit record.
/// </summary>
public class ReportSubscription
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public required string ReportCode { get; set; }
    public required string CronExpression { get; set; }
    public required string RecipientEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastRunAt { get; set; }
}
