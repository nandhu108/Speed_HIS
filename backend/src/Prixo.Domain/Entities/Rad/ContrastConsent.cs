using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Rad;

/// <summary>
/// Contrast consent capture (plan §12) — a dedicated, order-item-scoped record rather than
/// reusing the shared PatientConsent flow (which only links to a patient, not an order item).
/// Same shape as PatientConsent otherwise.
/// </summary>
public class ContrastConsent : BaseEntity
{
    public long RadOrderItemId { get; set; }
    public RadOrderItem? RadOrderItem { get; set; }

    public required string SignedByName { get; set; }
    public string? RelationshipToPatient { get; set; }
    public DateTimeOffset SignedAt { get; set; }
    /// <summary>Relative path to the scanned signed copy, if attached.</summary>
    public string? DocumentPath { get; set; }
}
