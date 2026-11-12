namespace Prixo.Domain.Enums;

/// <summary>Placeholder outbox channel — no sender is wired up until the Communication module (held).</summary>
public enum NotificationChannel
{
    Sms = 1,
    Email = 2,
    WhatsApp = 3,
}
