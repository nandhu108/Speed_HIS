using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Rad;

/// <summary>
/// Orderable radiology test — global catalog, same posture as Lab's LabTest. ModalityId
/// reuses the Modality master already seeded in Phase 2 (X-Ray/CT/MRI/USG/...).
/// </summary>
public class RadiologyTest : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public long ModalityId { get; set; }
    public Modality? Modality { get; set; }
    public required string BodyPart { get; set; }
    public bool RequiresContrast { get; set; }
    public string? PrepInstructions { get; set; }
    /// <summary>Links to the billable catalog for pricing; billing is a soft check, same as Lab.</summary>
    public long? ServiceId { get; set; }
    public Service? Service { get; set; }
    public int TatHours { get; set; } = 24;
}
