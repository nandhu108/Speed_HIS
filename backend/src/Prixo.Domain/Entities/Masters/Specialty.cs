using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Masters;

/// <summary>Medical specialty (Cardiology, Orthopaedics…). Global.</summary>
public class Specialty : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
