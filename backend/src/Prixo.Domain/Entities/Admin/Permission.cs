using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Admin;

/// <summary>
/// Seeded from the code registry (PermissionRegistry) — never created through the API.
/// Code format: MODULE.RESOURCE.ACTION.
/// </summary>
public class Permission : BaseEntity
{
    public required string Code { get; set; }
    public required string Module { get; set; }
    public required string Resource { get; set; }
    public required string Action { get; set; }
    public string? Description { get; set; }
}
