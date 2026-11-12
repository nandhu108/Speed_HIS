using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Masters;

/// <summary>Organizational unit (OPD, Pathology, Pharmacy…). Global — shared across branches.</summary>
public class Department : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    /// <summary>Clinical departments take patients (OP/IP); NonClinical are support/admin units.</summary>
    public DepartmentType Type { get; set; } = DepartmentType.Clinical;
}
