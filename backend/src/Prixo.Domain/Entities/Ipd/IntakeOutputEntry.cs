using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Ipd;

/// <summary>Free-text Category (Oral/IV/RT… for intake, Urine/Stool/Drain/Vomitus… for output) — no fixed master, kept simple for Phase 9.</summary>
public class IntakeOutputEntry : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public DateTimeOffset RecordedAt { get; set; }
    public bool IsIntake { get; set; }
    public required string Category { get; set; }
    public decimal QuantityMl { get; set; }
    public string? Notes { get; set; }
}
