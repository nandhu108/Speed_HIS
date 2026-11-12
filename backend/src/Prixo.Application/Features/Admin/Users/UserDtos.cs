namespace Prixo.Application.Features.Admin.Users;

public record UserListItemDto(
    long Id, string Username, string FullName, string? Email, string? Phone,
    string? EmployeeCode, string? DesignationName, string? DepartmentName, bool IsActive, bool IsLocked,
    DateTimeOffset? LastLoginAt, IReadOnlyList<string> Roles);

public record UserDetailDto(
    long Id, string Username, string FullName, string? Email, string? Phone,
    string? EmployeeCode, long? DesignationId, long? DepartmentId, string? PhotoPath, long? DefaultBranchId,
    bool IsActive, bool MustChangePassword, DateTimeOffset? LastLoginAt,
    IReadOnlyList<long> RoleIds, IReadOnlyList<long> BranchIds);

public record CreateUserRequest(
    string Username, string Password, string FullName, string? Email, string? Phone,
    string? EmployeeCode, long? DesignationId, long? DepartmentId, long? DefaultBranchId,
    IReadOnlyList<long> RoleIds, IReadOnlyList<long> BranchIds);

public record UpdateUserRequest(
    string FullName, string? Email, string? Phone, string? EmployeeCode,
    long? DesignationId, long? DepartmentId, long? DefaultBranchId, bool IsActive,
    IReadOnlyList<long> RoleIds, IReadOnlyList<long> BranchIds);

public record ResetPasswordRequest(string NewPassword, bool MustChangePassword = true);

public record PermissionOverrideDto(string PermissionCode, bool IsGranted);

/// <summary>RolePermissionCodes is what the user's roles alone would grant; Overrides are the per-user exceptions on top.</summary>
public record UserPermissionsDto(
    IReadOnlyList<string> RolePermissionCodes, IReadOnlyList<PermissionOverrideDto> Overrides);

public record SetUserPermissionOverridesRequest(IReadOnlyList<PermissionOverrideDto> Overrides);
