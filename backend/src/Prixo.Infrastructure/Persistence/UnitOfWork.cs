using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;

namespace Prixo.Infrastructure.Persistence;

/// <summary>
/// Wraps multi-table flows in a single transaction. No retrying execution strategy is
/// configured on the DbContext, so a plain BeginTransaction/Commit pair is sufficient.
/// </summary>
public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await action(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var result = await action(ct);
        await transaction.CommitAsync(ct);
        return result;
    }

    public async Task LockAsync<TEntity>(long id, CancellationToken ct = default) where TEntity : class
    {
        var entityType = db.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not an entity.");
        var table = entityType.GetTableName();
        var schema = entityType.GetSchema();
        var qualified = schema is null ? $"\"{table}\"" : $"\"{schema}\".\"{table}\"";
        // Identifiers come from the EF model (not user input); the id is a bound parameter.
        await db.Database.ExecuteSqlRawAsync($"SELECT 1 FROM {qualified} WHERE id = {{0}} FOR UPDATE", [id], ct);
    }
}
