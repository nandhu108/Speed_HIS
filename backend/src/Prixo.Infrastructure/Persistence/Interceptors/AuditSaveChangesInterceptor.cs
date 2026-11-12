using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Audit;

namespace Prixo.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps audit columns, converts hard deletes of ISoftDelete entities into
/// IsDeleted = true, and writes row-diff AuditLog rows (changed columns only).
/// Update/delete diffs join the same save batch; insert diffs need the generated
/// keys, so the interceptor opens a transaction and writes them right after the
/// main save, committing both together.
/// </summary>
public class AuditSaveChangesInterceptor(ICurrentUser currentUser, TimeProvider timeProvider)
    : SaveChangesInterceptor
{
    // Security-token/log tables are excluded: hashes must never be copied into the
    // audit trail, and logging the logs would recurse.
    private static readonly HashSet<Type> ExcludedEntities =
        [typeof(RefreshToken), typeof(UserSession), typeof(PasswordHistory)];

    private static readonly HashSet<string> ExcludedColumns =
    [
        nameof(User.PasswordHash),
        nameof(BaseEntity.CreatedAt), nameof(BaseEntity.CreatedBy),
        nameof(BaseEntity.UpdatedAt), nameof(BaseEntity.UpdatedBy),
    ];

    private readonly List<(EntityEntry Entry, AuditLog Log)> _pendingInserts = [];
    private IDbContextTransaction? _ownedTransaction;
    private bool _writingAuditRows;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (!_writingAuditRows && eventData.Context is { } context)
        {
            ApplyAudit(context);
            CollectAuditLogs(context);
            if (_pendingInserts.Count > 0 && context.Database.CurrentTransaction is null)
            {
                _ownedTransaction = context.Database.BeginTransaction();
            }
        }
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (!_writingAuditRows && eventData.Context is { } context)
        {
            ApplyAudit(context);
            CollectAuditLogs(context);
            if (_pendingInserts.Count > 0 && context.Database.CurrentTransaction is null)
            {
                _ownedTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
            }
        }
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (!_writingAuditRows && eventData.Context is { } context)
        {
            FlushPendingInsertAudits(context);
            _ownedTransaction?.Commit();
            _ownedTransaction?.Dispose();
            _ownedTransaction = null;
        }
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        if (!_writingAuditRows && eventData.Context is { } context)
        {
            await FlushPendingInsertAuditsAsync(context, cancellationToken);
            if (_ownedTransaction is not null)
            {
                await _ownedTransaction.CommitAsync(cancellationToken);
                await _ownedTransaction.DisposeAsync();
                _ownedTransaction = null;
            }
        }
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        DiscardPendingState();
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        DiscardPendingState();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void DiscardPendingState()
    {
        _pendingInserts.Clear();
        _ownedTransaction?.Dispose(); // rolls back if uncommitted
        _ownedTransaction = null;
    }

    private void ApplyAudit(DbContext context)
    {
        var now = timeProvider.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = currentUser.UserId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUser.UserId;
                    break;

                case EntityState.Deleted when entry.Entity is ISoftDelete soft:
                    entry.State = EntityState.Modified;
                    soft.IsDeleted = true;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUser.UserId;
                    break;
            }
        }
    }

    private void CollectAuditLogs(DbContext context)
    {
        // Snapshot first: adding AuditLog rows while iterating mutates the tracker.
        var entries = context.ChangeTracker.Entries<IAuditable>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified
                        && !ExcludedEntities.Contains(e.Entity.GetType()))
            .ToList();

        foreach (var entry in entries)
        {
            var log = BuildAuditLog(entry);
            if (log is null)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                _pendingInserts.Add((entry, log)); // key is generated by the database
            }
            else
            {
                log.EntityId = GetKeyString(entry);
                context.Set<AuditLog>().Add(log); // same batch, same transaction
            }
        }
    }

    private AuditLog? BuildAuditLog(EntityEntry entry)
    {
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            // Key values are temporary before the insert runs; EntityId carries the real key.
            if (ExcludedColumns.Contains(name) || property.Metadata.IsPrimaryKey())
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                newValues[name] = property.CurrentValue;
            }
            else if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
            {
                oldValues[name] = property.OriginalValue;
                newValues[name] = property.CurrentValue;
            }
        }

        var isSoftDelete = entry.State == EntityState.Modified
            && entry.Entity is ISoftDelete { IsDeleted: true }
            && newValues.TryGetValue(nameof(ISoftDelete.IsDeleted), out var deleted)
            && deleted is true;

        if (entry.State == EntityState.Modified && newValues.Count == 0)
        {
            return null; // nothing business-relevant changed
        }

        return new AuditLog
        {
            UserId = currentUser.UserId,
            BranchId = currentUser.BranchId,
            EntityName = entry.Entity.GetType().Name,
            EntityId = string.Empty, // filled in by the caller / after save
            Action = entry.State == EntityState.Added ? "Created" : isSoftDelete ? "Deleted" : "Updated",
            OldValues = entry.State == EntityState.Added ? null : JsonSerializer.Serialize(oldValues),
            NewValues = JsonSerializer.Serialize(newValues),
            TraceId = currentUser.TraceId,
            IpAddress = currentUser.IpAddress,
            CreatedAt = timeProvider.GetUtcNow(),
        };
    }

    private void FlushPendingInsertAudits(DbContext context)
    {
        if (_pendingInserts.Count == 0)
        {
            return;
        }
        AttachPendingInserts(context);
        _writingAuditRows = true;
        try
        {
            context.SaveChanges();
        }
        finally
        {
            _writingAuditRows = false;
        }
    }

    private async Task FlushPendingInsertAuditsAsync(DbContext context, CancellationToken ct)
    {
        if (_pendingInserts.Count == 0)
        {
            return;
        }
        AttachPendingInserts(context);
        _writingAuditRows = true;
        try
        {
            await context.SaveChangesAsync(ct);
        }
        finally
        {
            _writingAuditRows = false;
        }
    }

    private void AttachPendingInserts(DbContext context)
    {
        foreach (var (entry, log) in _pendingInserts)
        {
            log.EntityId = GetKeyString(entry);
            context.Set<AuditLog>().Add(log);
        }
        _pendingInserts.Clear();
    }

    private static string GetKeyString(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null)
        {
            return string.Empty;
        }
        return string.Join(",", key.Properties.Select(p => entry.Property(p.Name).CurrentValue));
    }
}
