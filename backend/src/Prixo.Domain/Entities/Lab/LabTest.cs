using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Lab;

/// <summary>
/// Orderable lab test — global catalog. IsPanel tests (e.g. CBC, LFT) carry several
/// LabTestParameter rows; single-parameter tests still get exactly one parameter row so
/// result entry/reference-range resolution never has to special-case the non-panel path.
/// </summary>
public class LabTest : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public long SampleTypeId { get; set; }
    public SampleType? SampleType { get; set; }
    /// <summary>Links to the billable catalog for pricing; billing is a soft check this phase (see docs/modules/lab.md).</summary>
    public long? ServiceId { get; set; }
    public Service? Service { get; set; }
    public string? Method { get; set; }
    public int TatHours { get; set; } = 24;
    public bool IsPanel { get; set; }

    public ICollection<LabTestParameter> Parameters { get; set; } = [];
}

public class LabTestParameter : BaseEntity
{
    public long LabTestId { get; set; }
    public LabTest? LabTest { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Unit { get; set; }
    public ParameterValueType ValueType { get; set; } = ParameterValueType.Numeric;
    /// <summary>Comma-separated options — only meaningful when ValueType is Dropdown.</summary>
    public string? DropdownOptions { get; set; }
    public int SortOrder { get; set; }

    public ICollection<ReferenceRange> ReferenceRanges { get; set; } = [];
}

/// <summary>
/// One row per age/sex bracket for a parameter. Null Sex/age bounds mean "applies to
/// everyone" / "no bound" respectively — resolution picks the most specific match, same
/// specific-beats-general idiom as Billing's tariff resolution.
/// </summary>
public class ReferenceRange : BaseEntity
{
    public long LabTestParameterId { get; set; }
    public LabTestParameter? LabTestParameter { get; set; }
    public Gender? Sex { get; set; }
    public int? MinAgeYears { get; set; }
    public int? MaxAgeYears { get; set; }
    public decimal? NormalLow { get; set; }
    public decimal? NormalHigh { get; set; }
    public decimal? CriticalLow { get; set; }
    public decimal? CriticalHigh { get; set; }
    /// <summary>Expected/normal value for Text-type parameters, where numeric bounds don't apply.</summary>
    public string? TextNormalValue { get; set; }
}
