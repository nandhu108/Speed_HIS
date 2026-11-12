namespace Prixo.Application.Common.Interfaces;

/// <summary>
/// Wraps multi-table flows in a single database transaction. Mandatory for billing,
/// payments/refunds, pharmacy sale/issue, GRN, admission/discharge, stock
/// transfer/adjustment, day close — and patient merge (re-points FKs transactionally).
/// </summary>
public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default);

    /// <summary>
    /// Takes a row-level write lock (<c>SELECT … FOR UPDATE</c>) on one entity row for the rest of
    /// the current transaction, serialising concurrent writers (e.g. two payments on one bill, two
    /// sales of the last units of a batch). Must be called inside <see cref="ExecuteInTransactionAsync"/>
    /// and BEFORE the row is read, so the read sees the committed state of any competing writer.
    /// </summary>
    Task LockAsync<TEntity>(long id, CancellationToken ct = default) where TEntity : class;
}
