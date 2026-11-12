using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Admin;

namespace Prixo.Application.Features.Admin.Sessions;

public record SessionDto(
    long Id, Guid SessionId, long UserId, string Username, string FullName,
    long BranchId, string? BranchName, string? IpAddress, string? UserAgent,
    DateTimeOffset LoginAt, DateTimeOffset LastActivityAt, DateTimeOffset? LoggedOutAt,
    bool IsRevoked, bool IsCurrent);

public class SessionService(
    IAppDbContext db, IActivityLogger activity, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<PagedResult<SessionDto>> GetPagedAsync(
        PagedQuery query, bool activeOnly, CancellationToken ct = default)
    {
        var sessions = db.UserSessions.AsNoTracking();

        if (activeOnly)
        {
            sessions = sessions.Where(s => !s.IsRevoked && s.LoggedOutAt == null);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            sessions = sessions.Where(s =>
                EF.Functions.ILike(s.User.Username, term) ||
                EF.Functions.ILike(s.User.FullName, term));
        }

        var totalCount = await sessions.CountAsync(ct);
        var currentSessionId = currentUser.SessionId;

        var items = await sessions
            .OrderByDescending(s => s.LastActivityAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new SessionDto(
                s.Id, s.SessionId, s.UserId, s.User.Username, s.User.FullName,
                s.BranchId, db.Branches.Where(b => b.Id == s.BranchId).Select(b => b.Name).FirstOrDefault(),
                s.IpAddress, s.UserAgent, s.LoginAt, s.LastActivityAt, s.LoggedOutAt,
                s.IsRevoked, s.SessionId == currentSessionId))
            .ToListAsync(ct);

        return PagedResult<SessionDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    /// <summary>Force-logout: revokes the session and every refresh token in its family.</summary>
    public async Task RevokeAsync(Guid sessionId, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();

        var session = await db.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct)
            ?? throw new NotFoundException(nameof(UserSession), sessionId);

        if (!session.IsRevoked && session.LoggedOutAt is null)
        {
            session.IsRevoked = true;
            session.LoggedOutAt = now;

            var activeTokens = await db.RefreshTokens
                .Where(t => t.SessionId == sessionId && t.RevokedAt == null)
                .ToListAsync(ct);
            foreach (var token in activeTokens)
            {
                token.RevokedAt = now;
                token.ReplacedByTokenHash = "revoked: forced logout";
            }
        }

        await activity.LogAsync(ActivityEvents.SessionRevoked,
            $"Session of '{session.User.Username}' revoked by '{currentUser.UserName}'",
            session.UserId, session.User.Username, ct: ct);
        await db.SaveChangesAsync(ct);
    }
}
