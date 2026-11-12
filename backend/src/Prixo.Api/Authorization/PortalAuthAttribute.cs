using Microsoft.AspNetCore.Authorization;

namespace Prixo.Api.Authorization;

/// <summary>Requires the Patient Portal's JWT scheme/audience (plan §21) — never the staff scheme. Every non-[AllowAnonymous] portal endpoint carries this instead of [HasPermission] (patients have no RBAC permissions).</summary>
public class PortalAuthAttribute : AuthorizeAttribute
{
    public const string SchemeName = "Portal";

    public PortalAuthAttribute()
    {
        AuthenticationSchemes = SchemeName;
    }
}
