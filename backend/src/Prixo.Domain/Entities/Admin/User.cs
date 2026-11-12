using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Admin;

public class User : BaseEntity
{
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? EmployeeCode { get; set; }
    public long? DesignationId { get; set; }
    public Designation? Designation { get; set; }
    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }
    /// <summary>Stored path/URL only; the upload endpoint arrives with file storage later.</summary>
    public string? PhotoPath { get; set; }
    public long? DefaultBranchId { get; set; }

    public bool MustChangePassword { get; set; } = true;
    public DateTimeOffset? PasswordChangedAt { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<UserBranch> UserBranches { get; set; } = [];
}
