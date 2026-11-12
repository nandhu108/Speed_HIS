using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Ot;

/// <summary>A physical operation theatre within a branch — the resource booking conflict-checks against.</summary>
public class Theatre : BranchEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
}

/// <summary>Equipment available in a theatre — informational only, not booked/reserved at a granular level.</summary>
public class Equipment : BranchEntity
{
    public long? TheatreId { get; set; }
    public Theatre? Theatre { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
}
