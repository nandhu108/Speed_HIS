using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Pharmacy;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Pharmacy;

/// <summary>
/// Updates a batch's running balance and appends the matching StockLedger row in one
/// call — every stock-writing service uses this so "update balance" and "log the
/// movement" can never drift apart. Uses the Batch navigation (not BatchId) so it works
/// even when the batch was just added and hasn't been saved yet — EF fixes up the FK at
/// SaveChanges. ReferenceId, by contrast, is a plain long (not a FK/nav — StockLedger
/// references many different document types), so the caller's document must already have
/// its own Id before calling this.
/// </summary>
internal static class StockLedgerWriter
{
    public static StockLedger Write(
        IAppDbContext db, Batch batch, StockMovementType type, decimal quantity,
        string referenceType, long referenceId, DateTimeOffset movementAt, string? notes = null)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Stock movement quantity must be greater than zero.");
        }
        var newBalance = StockMath.ApplyMovement(batch.QuantityOnHand, type, quantity);
        if (newBalance < 0)
        {
            // Single choke point for every outbound movement (sale, issue, adjustment, transfer,
            // purchase return, OT usage): stock can never go below zero.
            throw new DomainException(
                $"Insufficient stock in batch {batch.BatchNumber}: {batch.QuantityOnHand} on hand, {quantity} requested.");
        }
        batch.QuantityOnHand = newBalance;
        var entry = new StockLedger
        {
            BranchId = batch.BranchId,
            StoreId = batch.StoreId,
            Batch = batch,
            MovementType = type,
            Quantity = quantity,
            RunningBalance = batch.QuantityOnHand,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            MovementAt = movementAt,
            Notes = notes,
        };
        db.StockLedgers.Add(entry);
        return entry;
    }
}
