using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Audit;
using Prixo.Infrastructure.Persistence;

namespace Prixo.Infrastructure.Services;

public class ActivityLogger(AppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
    : IActivityLogger
{
    public async Task LogAsync(string eventType, string description,
        long? userId = null, string? username = null,
        bool saveImmediately = false, CancellationToken ct = default)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId ?? (currentUser.IsAuthenticated ? currentUser.UserId : null),
            Username = username ?? currentUser.UserName,
            BranchId = currentUser.BranchId,
            EventType = eventType,
            Description = description,
            IpAddress = currentUser.IpAddress,
            UserAgent = currentUser.UserAgent,
            CreatedAt = timeProvider.GetUtcNow(),
        });

        if (saveImmediately)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
