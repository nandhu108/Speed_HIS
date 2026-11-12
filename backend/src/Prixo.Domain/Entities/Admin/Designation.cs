using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Admin;

public class Designation : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}
