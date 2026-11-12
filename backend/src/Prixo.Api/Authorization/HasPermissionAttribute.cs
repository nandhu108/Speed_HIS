using Microsoft.AspNetCore.Authorization;

namespace Prixo.Api.Authorization;

/// <summary>
/// Declares the MODULE.RESOURCE.ACTION permission an endpoint requires. Resolved to a
/// dynamic policy by <see cref="PermissionPolicyProvider"/> and checked against the
/// caller's effective permissions in the database — permissions are NOT in the JWT.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "PERMISSION:";

    public HasPermissionAttribute(string permissionCode)
    {
        PermissionCode = permissionCode;
        Policy = PolicyPrefix + permissionCode;
    }

    public string PermissionCode { get; }
}
