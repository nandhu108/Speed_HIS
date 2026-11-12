using System.Security.Claims;
using Prixo.Application.Common.Interfaces;

namespace Prixo.Api.Services;

public class PortalCurrentUserService(IHttpContextAccessor httpContextAccessor) : IPortalCurrentUser
{
    private HttpContext? Context => httpContextAccessor.HttpContext;
    private ClaimsPrincipal? Principal => Context?.User;

    /// <summary>True only when the "portal" claim is present — guards against a stray staff-scheme principal being read as a portal one.</summary>
    public bool IsAuthenticated =>
        (Principal?.Identity?.IsAuthenticated ?? false) && Principal!.FindFirstValue("portal") == "true";

    public long PatientId =>
        IsAuthenticated && long.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public Guid? SessionId =>
        Guid.TryParse(Principal?.FindFirstValue("sid"), out var sessionId) ? sessionId : null;

    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => Context?.Request.Headers.UserAgent.ToString();
}
