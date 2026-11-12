using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Features.Admin.Settings;

namespace Prixo.Api.Middleware;

/// <summary>
/// Rejects access tokens whose server-side session was revoked or idle-expired
/// (JWTs alone would stay valid for their full 15 minutes), and keeps
/// LastActivityAt fresh — throttled to one write per minute per session.
/// </summary>
public class SessionValidationMiddleware(RequestDelegate next)
{
    private static readonly TimeSpan ActivityWriteThrottle = TimeSpan.FromMinutes(1);

    public async Task InvokeAsync(
        HttpContext context, ICurrentUser currentUser, IAppDbContext db,
        SettingService settings, TimeProvider timeProvider)
    {
        if (!currentUser.IsAuthenticated || currentUser.SessionId is not { } sessionId)
        {
            await next(context);
            return;
        }

        var session = await db.UserSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, context.RequestAborted);
        if (session is null || session.IsRevoked || session.LoggedOutAt is not null)
        {
            await Reject(context, "Session has ended. Sign in again.");
            return;
        }

        var now = timeProvider.GetUtcNow();
        var idleMinutes = await settings.GetIntAsync(
            SettingKeys.SessionIdleTimeoutMinutes, 30, context.RequestAborted);
        if (idleMinutes > 0 && session.LastActivityAt.AddMinutes(idleMinutes) < now)
        {
            session.IsRevoked = true;
            session.LoggedOutAt = now;
            await db.SaveChangesAsync(context.RequestAborted);
            await Reject(context, "Session expired due to inactivity. Sign in again.");
            return;
        }

        if (now - session.LastActivityAt >= ActivityWriteThrottle)
        {
            session.LastActivityAt = now;
            await db.SaveChangesAsync(context.RequestAborted);
        }

        await next(context);
    }

    private static async Task Reject(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail(message, traceId: context.TraceIdentifier));
    }
}
