using System.Security.Claims;
using Prixo.Application.Common.Interfaces;

namespace Prixo.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    // Seeded system user; actor for unauthenticated and background operations.
    private const long SystemUserId = 1;

    private HttpContext? Context => httpContextAccessor.HttpContext;
    private ClaimsPrincipal? Principal => Context?.User;

    /// <summary>True when a Patient Portal token (plan §21's separate JWT audience) is on the request — its "sub" is a PatientId, never a staff UserId, so every property below must refuse to trust it.</summary>
    private bool IsPortalPrincipal => Principal?.FindFirstValue("portal") == "true";

    public bool IsAuthenticated => !IsPortalPrincipal && (Principal?.Identity?.IsAuthenticated ?? false);

    public long UserId =>
        !IsPortalPrincipal && long.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : SystemUserId;

    public long? BranchId =>
        !IsPortalPrincipal && long.TryParse(Principal?.FindFirstValue("branch"), out var branchId)
            ? branchId
            : null;

    public string? UserName => Principal?.FindFirstValue(ClaimTypes.Name);

    public Guid? SessionId =>
        Guid.TryParse(Principal?.FindFirstValue("sid"), out var sessionId)
            ? sessionId
            : null;

    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => Context?.Request.Headers.UserAgent.ToString();

    public string? TraceId => Context?.TraceIdentifier;
}
