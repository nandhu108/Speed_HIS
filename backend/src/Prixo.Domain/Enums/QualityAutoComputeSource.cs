namespace Prixo.Domain.Enums;

/// <summary>Which existing engine an indicator entry's numerator/denominator is auto-pulled from (plan §20: "auto-pull where computable"). Null on the indicator means manual entry only.</summary>
public enum QualityAutoComputeSource
{
    LabTat = 1,
    BedOccupancy = 2,
    IncidentCount = 3,
}
