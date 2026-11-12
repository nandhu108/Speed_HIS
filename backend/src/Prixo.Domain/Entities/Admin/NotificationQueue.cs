using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Admin;

/// <summary>
/// Outbox placeholder (plan §Phase 4: "Appointment Reminder — placeholder, no sender").
/// Rows are enqueued but never dispatched until the Communication module (held) adds a
/// sender. Not a BaseEntity — infra table, not a business/audit record.
/// </summary>
public class NotificationQueue
{
    public long Id { get; set; }
    public NotificationChannel Channel { get; set; }
    public required string Recipient { get; set; }
    public string? Subject { get; set; }
    public required string Body { get; set; }
    public string? RelatedEntityType { get; set; }
    public long? RelatedEntityId { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}
