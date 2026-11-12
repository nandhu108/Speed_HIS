using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Masters;

/// <summary>Service grouping (Consultation, Procedure, Investigation…). Global.</summary>
public class ServiceCategory : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Chargeable service in the global catalog. Branch pricing comes from tariffs;
/// DefaultRate is the fallback when no tariff row matches.
/// </summary>
public class Service : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public long ServiceCategoryId { get; set; }
    public ServiceCategory? Category { get; set; }
    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public bool IsTaxable { get; set; }
    public long? TaxId { get; set; }
    public Tax? Tax { get; set; }
    public decimal DefaultRate { get; set; }
}
