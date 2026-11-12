using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Features.Portal;

namespace Prixo.Api.Controllers.Portal;

[Route("api/v1/portal/auth")]
[EnableRateLimiting("PortalAuth")]
public class PortalAuthController(PortalAuthService auth) : ApiControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse>> Register(RegisterPortalAccountRequest request, CancellationToken ct)
    {
        await auth.RegisterAsync(request, ct);
        return OkResponse<object?>(null, "Portal account activated. You can now log in.");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PortalLoginResponse>>> Login(PortalLoginRequest request, CancellationToken ct) =>
        OkResponse(await auth.LoginAsync(request, ct), "Login successful.");

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PortalLoginResponse>>> Refresh(PortalRefreshRequest request, CancellationToken ct) =>
        OkResponse(await auth.RefreshAsync(request, ct));

    [HttpPost("logout")]
    [PortalAuth]
    public async Task<ActionResult<ApiResponse>> Logout(CancellationToken ct)
    {
        await auth.LogoutAsync(ct);
        return OkResponse<object?>(null, "Logged out.");
    }

    [HttpPost("change-password")]
    [PortalAuth]
    public async Task<ActionResult<ApiResponse>> ChangePassword(PortalChangePasswordRequest request, CancellationToken ct)
    {
        await auth.ChangePasswordAsync(request, ct);
        return OkResponse<object?>(null, "Password changed.");
    }
}
