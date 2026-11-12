using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;

namespace Prixo.Domain.Entities.Masters;

/// <summary>
/// Lightweight staff record for non-login personnel (full HR &amp; Payroll is a held
/// module). Branch-scoped — staff belong to a hospital unit.
/// </summary>
public class Employee : BranchEntity
{
    public required string Code { get; set; }
    public required string FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public long? DesignationId { get; set; }
    public Designation? Designation { get; set; }
    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public DateOnly? JoinedOn { get; set; }
}
