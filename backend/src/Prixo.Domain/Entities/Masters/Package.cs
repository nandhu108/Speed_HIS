using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Masters;

/// <summary>
/// Bundle of services sold at a fixed price (health checks, surgery packages).
/// Branch-scoped — packages and their prices differ per unit.
/// </summary>
public class Package : BranchEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    /// <summary>Days the package stays usable after purchase (0 = no expiry).</summary>
    public int ValidityDays { get; set; }

    public ICollection<PackageItem> Items { get; set; } = [];
}

public class PackageItem : BaseEntity
{
    public long PackageId { get; set; }
    public Package? Package { get; set; }
    public long ServiceId { get; set; }
    public Service? Service { get; set; }
    public decimal Quantity { get; set; } = 1;
}
