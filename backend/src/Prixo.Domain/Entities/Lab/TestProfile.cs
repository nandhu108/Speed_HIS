using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Lab;

/// <summary>Orderable bundle of tests (e.g. "Liver Function Test"). Ordering a profile expands to one LabOrderItem per member test.</summary>
public class TestProfile : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    /// <summary>Bundle price override; null falls back to summing member tests' resolved service rates.</summary>
    public decimal? Price { get; set; }

    public ICollection<ProfileTest> Tests { get; set; } = [];
}

public class ProfileTest : BaseEntity
{
    public long TestProfileId { get; set; }
    public TestProfile? TestProfile { get; set; }
    public long LabTestId { get; set; }
    public LabTest? LabTest { get; set; }
}
