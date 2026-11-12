using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Features.Admin.Auth;

namespace Prixo.Api.Controllers;

public class AuthController(AuthService auth, IAppDbContext db, ICurrentUser currentUser)
    : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("StaffAuth")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        LoginRequest request, CancellationToken ct) =>
        OkResponse(await auth.LoginAsync(request, ct), "Signed in.");

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("StaffAuth")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Refresh(
        RefreshRequest request, CancellationToken ct) =>
        OkResponse(await auth.RefreshAsync(request, ct));

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse>> Logout(CancellationToken ct)
    {
        await auth.LogoutAsync(ct);
        return OkResponse<object?>(null, "Signed out.");
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<AuthUserDto>>> Me(CancellationToken ct) =>
        OkResponse(await auth.GetMeAsync(ct));

    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse>> ChangePassword(
        ChangePasswordRequest request, CancellationToken ct)
    {
        await auth.ChangePasswordAsync(request, ct);
        return OkResponse<object?>(null, "Password changed.");
    }

    /// <summary>Branches the signed-in user may switch to (re-login with BranchId).</summary>
    [HttpGet("my-branches")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BranchOption>>>> MyBranches(CancellationToken ct)
    {
        var branches = await db.UserBranches.AsNoTracking()
            .Where(ub => ub.UserId == currentUser.UserId && ub.Branch.IsActive)
            .OrderBy(ub => ub.Branch.Name)
            .Select(ub => new BranchOption(ub.Branch.Id, ub.Branch.Code, ub.Branch.Name))
            .ToListAsync(ct);
        return OkResponse<IReadOnlyList<BranchOption>>(branches);
    }
}
