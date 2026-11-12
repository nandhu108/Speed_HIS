using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Pharmacy;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Pharmacy;

/// <summary>
/// Stock explorer, adjustments (the one true approval-gated workflow this phase),
/// transfers (store↔store / branch↔branch), audits (count sheet → variance posting), and
/// the near-expiry/low-stock/consumption queries. Adjustment approval and transfer
/// dispatch/receive are mandatory transactions (CLAUDE.md).
/// </summary>
public class StockService(
    IAppDbContext db, IUnitOfWork unitOfWork, ISequenceService sequences,
    ICurrentUser currentUser, IActivityLogger activity, TimeProvider timeProvider)
{
    // ── Stock explorer ───────────────────────────────────────────────────────

    public async Task<IReadOnlyList<BatchDto>> GetBatchesAsync(long? drugId, long? itemId, long? storeId, bool onlyInStock, CancellationToken ct = default)
    {
        var batches = db.Batches.AsNoTracking();
        if (drugId is { } d) batches = batches.Where(b => b.DrugId == d);
        if (itemId is { } i) batches = batches.Where(b => b.ItemId == i);
        if (storeId is { } s) batches = batches.Where(b => b.StoreId == s);
        if (onlyInStock) batches = batches.Where(b => b.QuantityOnHand > 0);

        return await batches.Include(b => b.Drug).Include(b => b.Item).Include(b => b.Store)
            .OrderBy(b => b.Drug != null ? b.Drug.Name : b.Item!.Name).ThenBy(b => b.ExpiryDate)
            .Select(b => new BatchDto(b.Id, b.DrugId, b.ItemId, b.Drug != null ? b.Drug.Name : b.Item!.Name,
                b.StoreId, b.Store!.Name, b.BatchNumber, b.ExpiryDate, b.Mrp, b.PurchaseRate, b.QuantityOnHand))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StockLedgerEntryDto>> GetLedgerAsync(long batchId, CancellationToken ct = default) =>
        await db.StockLedgers.AsNoTracking()
            .Where(l => l.BatchId == batchId)
            .OrderByDescending(l => l.MovementAt).ThenByDescending(l => l.Id)
            .Select(l => new StockLedgerEntryDto(l.Id, l.MovementType, l.Quantity, l.RunningBalance, l.ReferenceType, l.ReferenceId, l.MovementAt, l.Notes))
            .ToListAsync(ct);

    // ── Stock adjustment ─────────────────────────────────────────────────────

    public async Task<long> CreateAdjustmentAsync(CreateStockAdjustmentRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        if (!await db.Stores.AnyAsync(s => s.Id == request.StoreId, ct))
        {
            throw new NotFoundException(nameof(Domain.Entities.Pharmacy.Store), request.StoreId);
        }
        if (request.Items.Count == 0)
        {
            throw new DomainException("Add at least one batch to the adjustment.");
        }
        foreach (var line in request.Items)
        {
            if (line.QuantityChange == 0)
            {
                throw new DomainException("Adjustment quantity cannot be zero.");
            }
            var onHand = await db.Batches.Where(b => b.Id == line.BatchId && b.StoreId == request.StoreId)
                .Select(b => (decimal?)b.QuantityOnHand).FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException(nameof(Batch), line.BatchId);
            if (onHand + line.QuantityChange < 0)
            {
                throw new DomainException($"Adjustment of {line.QuantityChange} would take the batch below zero ({onHand} on hand).");
            }
        }

        var today = Today();
        var number = await sequences.NextValueAsync(SequenceKeys.StockAdjustmentNumber, branchId, today.Year, ct);
        var adjustment = new StockAdjustment
        {
            BranchId = branchId,
            StoreId = request.StoreId,
            AdjustmentNumber = $"ADJ-{today.Year}-{number.ToString().PadLeft(6, '0')}",
            AdjustmentDate = today,
            Reason = request.Reason,
            Items = request.Items.Select(i => new StockAdjustmentItem { BatchId = i.BatchId, QuantityChange = i.QuantityChange }).ToList(),
        };
        db.StockAdjustments.Add(adjustment);
        await activity.LogAsync(ActivityEvents.StockAdjustmentRequested, $"Stock adjustment {adjustment.AdjustmentNumber} requested: {request.Reason}", ct: ct);
        await db.SaveChangesAsync(ct);
        return adjustment.Id;
    }

    public async Task<IReadOnlyList<StockAdjustmentListItemDto>> GetPendingAdjustmentsAsync(CancellationToken ct = default) =>
        await db.StockAdjustments.AsNoTracking()
            .Where(a => a.Status == StockAdjustmentStatus.Pending)
            .Include(a => a.Store)
            .OrderBy(a => a.AdjustmentDate)
            .Select(a => new StockAdjustmentListItemDto(a.Id, a.AdjustmentNumber, a.AdjustmentDate, a.Store!.Name, a.Status))
            .ToListAsync(ct);

    public async Task<StockAdjustmentDetailDto> GetAdjustmentByIdAsync(long id, CancellationToken ct = default)
    {
        var a = await db.StockAdjustments.AsNoTracking().Include(x => x.Store)
            .Include(x => x.Items).ThenInclude(i => i.Batch).ThenInclude(b => b!.Drug)
            .Include(x => x.Items).ThenInclude(i => i.Batch).ThenInclude(b => b!.Item)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(StockAdjustment), id);
        return new StockAdjustmentDetailDto(a.Id, a.AdjustmentNumber, a.AdjustmentDate, a.StoreId, a.Store!.Name, a.Reason,
            a.Status, a.RejectionReason,
            a.Items.Select(i => new StockAdjustmentItemDto(i.Id, i.BatchId, ProductNaming.Of(i.Batch!), i.Batch!.BatchNumber, i.QuantityChange)).ToList());
    }

    /// <summary>Approving writes the StockLedger rows (adjustment-in for positive changes, adjustment-out for negative) — mandatory transaction.</summary>
    public async Task DecideAdjustmentAsync(long id, DecideStockAdjustmentRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await unitOfWork.LockAsync<StockAdjustment>(id, token);
            var adjustment = await db.StockAdjustments.Include(a => a.Items).ThenInclude(i => i.Batch)
                .FirstOrDefaultAsync(a => a.Id == id, token) ?? throw new NotFoundException(nameof(StockAdjustment), id);
            if (adjustment.Status != StockAdjustmentStatus.Pending)
            {
                throw new DomainException($"An adjustment that is {adjustment.Status} cannot be decided again.");
            }
            if (adjustment.CreatedBy == currentUser.UserId)
            {
                // Maker-checker: a stock write-off/write-on must be approved by someone other than
                // the person who raised it.
                throw new DomainException("You raised this adjustment; it must be approved or rejected by another user.");
            }

            if (request.Approve)
            {
                var now = timeProvider.GetUtcNow();
                foreach (var item in adjustment.Items)
                {
                    var type = item.QuantityChange >= 0 ? StockMovementType.AdjustmentIn : StockMovementType.AdjustmentOut;
                    StockLedgerWriter.Write(db, item.Batch!, type, Math.Abs(item.QuantityChange), nameof(StockAdjustment), adjustment.Id, now);
                }
                adjustment.Status = StockAdjustmentStatus.Approved;
                await activity.LogAsync(ActivityEvents.StockAdjustmentApproved, $"Stock adjustment {adjustment.AdjustmentNumber} approved", ct: token);
            }
            else
            {
                adjustment.Status = StockAdjustmentStatus.Rejected;
                adjustment.RejectionReason = request.RejectionReason;
                await activity.LogAsync(ActivityEvents.StockAdjustmentRejected, $"Stock adjustment {adjustment.AdjustmentNumber} rejected: {request.RejectionReason}", ct: token);
            }
            adjustment.DecidedByUserId = currentUser.UserId;
            adjustment.DecidedAt = timeProvider.GetUtcNow();
            await db.SaveChangesAsync(token);
        }, ct);

    // ── Stock transfer ───────────────────────────────────────────────────────

    public async Task<long> CreateTransferAsync(CreateStockTransferRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        if (!await db.Stores.AnyAsync(s => s.Id == request.FromStoreId, ct))
        {
            throw new NotFoundException(nameof(Domain.Entities.Pharmacy.Store), request.FromStoreId);
        }
        if (!await db.Stores.AnyAsync(s => s.Id == request.ToStoreId, ct))
        {
            throw new NotFoundException(nameof(Domain.Entities.Pharmacy.Store), request.ToStoreId);
        }
        if (request.Items.Count == 0)
        {
            throw new DomainException("Add at least one batch to the transfer.");
        }

        var today = Today();
        var number = await sequences.NextValueAsync(SequenceKeys.StockTransferNumber, branchId, today.Year, ct);
        var transfer = new StockTransfer
        {
            BranchId = branchId,
            FromStoreId = request.FromStoreId,
            ToBranchId = request.ToBranchId,
            ToStoreId = request.ToStoreId,
            TransferNumber = $"TRF-{today.Year}-{number.ToString().PadLeft(6, '0')}",
            TransferDate = today,
            Items = request.Items.Select(i => new StockTransferItem { BatchId = i.BatchId, Quantity = i.Quantity }).ToList(),
        };
        db.StockTransfers.Add(transfer);
        await db.SaveChangesAsync(ct);
        return transfer.Id;
    }

    /// <summary>Writes TransferOut at the source store — mandatory transaction.</summary>
    public async Task DispatchTransferAsync(long id, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var transfer = await db.StockTransfers.Include(t => t.Items).ThenInclude(i => i.Batch)
                .FirstOrDefaultAsync(t => t.Id == id, token) ?? throw new NotFoundException(nameof(StockTransfer), id);
            if (transfer.Status != StockTransferStatus.Pending)
            {
                throw new DomainException($"A transfer that is {transfer.Status} cannot be dispatched.");
            }

            var now = timeProvider.GetUtcNow();
            foreach (var item in transfer.Items)
            {
                if (item.Batch!.QuantityOnHand < item.Quantity)
                {
                    throw new DomainException($"Batch {item.Batch.BatchNumber} only has {item.Batch.QuantityOnHand} on hand.");
                }
                StockLedgerWriter.Write(db, item.Batch, StockMovementType.TransferOut, item.Quantity, nameof(StockTransfer), transfer.Id, now);
            }
            transfer.Status = StockTransferStatus.Dispatched;
            transfer.DispatchedAt = now;
            await activity.LogAsync(ActivityEvents.StockTransferDispatched, $"Transfer {transfer.TransferNumber} dispatched", ct: token);
            await db.SaveChangesAsync(token);
        }, ct);

    /// <summary>Creates/tops up the destination batch and writes TransferIn there — mandatory transaction.</summary>
    public async Task ReceiveTransferAsync(long id, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var transfer = await db.StockTransfers.Include(t => t.Items).ThenInclude(i => i.Batch)
                .FirstOrDefaultAsync(t => t.Id == id, token) ?? throw new NotFoundException(nameof(StockTransfer), id);
            if (transfer.Status != StockTransferStatus.Dispatched)
            {
                throw new DomainException($"A transfer that is {transfer.Status} cannot be received.");
            }

            var now = timeProvider.GetUtcNow();
            foreach (var item in transfer.Items)
            {
                var sourceBatch = item.Batch!;
                var destinationBatch = await db.Batches.FirstOrDefaultAsync(b =>
                    b.DrugId == sourceBatch.DrugId && b.ItemId == sourceBatch.ItemId
                    && b.StoreId == transfer.ToStoreId && b.BatchNumber == sourceBatch.BatchNumber, token);
                if (destinationBatch is null)
                {
                    destinationBatch = new Batch
                    {
                        BranchId = transfer.ToBranchId,
                        DrugId = sourceBatch.DrugId,
                        ItemId = sourceBatch.ItemId,
                        StoreId = transfer.ToStoreId,
                        BatchNumber = sourceBatch.BatchNumber,
                        ExpiryDate = sourceBatch.ExpiryDate,
                        Mrp = sourceBatch.Mrp,
                        PurchaseRate = sourceBatch.PurchaseRate,
                    };
                    db.Batches.Add(destinationBatch);
                }
                StockLedgerWriter.Write(db, destinationBatch, StockMovementType.TransferIn, item.Quantity, nameof(StockTransfer), transfer.Id, now);
                item.ReceivedBatch = destinationBatch;
            }
            transfer.Status = StockTransferStatus.Received;
            transfer.ReceivedAt = now;
            await activity.LogAsync(ActivityEvents.StockTransferReceived, $"Transfer {transfer.TransferNumber} received", ct: token);
            await db.SaveChangesAsync(token);
        }, ct);

    public async Task<IReadOnlyList<StockTransferListItemDto>> GetTransfersAsync(CancellationToken ct = default) =>
        await db.StockTransfers.AsNoTracking()
            .Include(t => t.FromStore).Include(t => t.ToStore)
            .OrderByDescending(t => t.TransferDate).ThenByDescending(t => t.Id)
            .Select(t => new StockTransferListItemDto(t.Id, t.TransferNumber, t.TransferDate, t.FromStore!.Name, t.ToStore!.Name, t.Status))
            .ToListAsync(ct);

    public async Task<StockTransferDetailDto> GetTransferByIdAsync(long id, CancellationToken ct = default)
    {
        var t = await db.StockTransfers.AsNoTracking()
            .Include(x => x.FromStore).Include(x => x.ToStore).Include(x => x.ToBranch)
            .Include(x => x.Items).ThenInclude(i => i.Batch).ThenInclude(b => b!.Drug)
            .Include(x => x.Items).ThenInclude(i => i.Batch).ThenInclude(b => b!.Item)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(StockTransfer), id);
        return new StockTransferDetailDto(t.Id, t.TransferNumber, t.TransferDate, t.FromStoreId, t.FromStore!.Name,
            t.ToBranchId, t.ToBranch!.Name, t.ToStoreId, t.ToStore!.Name, t.Status,
            t.Items.Select(i => new StockTransferItemDto(i.Id, i.BatchId, ProductNaming.Of(i.Batch!), i.Batch!.BatchNumber, i.Quantity)).ToList());
    }

    // ── Stock audit ───────────────────────────────────────────────────────────

    public async Task<long> CreateAuditAsync(CreateStockAuditRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        if (!await db.Stores.AnyAsync(s => s.Id == request.StoreId, ct))
        {
            throw new NotFoundException(nameof(Domain.Entities.Pharmacy.Store), request.StoreId);
        }

        var today = Today();
        var number = await sequences.NextValueAsync(SequenceKeys.StockAuditNumber, branchId, today.Year, ct);
        var batches = await db.Batches.Where(b => b.StoreId == request.StoreId && b.QuantityOnHand > 0).ToListAsync(ct);

        var audit = new StockAudit
        {
            BranchId = branchId,
            StoreId = request.StoreId,
            AuditNumber = $"AUD-{today.Year}-{number.ToString().PadLeft(6, '0')}",
            AuditDate = today,
            Items = batches.Select(b => new StockAuditItem { BatchId = b.Id, SystemQuantity = b.QuantityOnHand, CountedQuantity = b.QuantityOnHand }).ToList(),
        };
        db.StockAudits.Add(audit);
        await db.SaveChangesAsync(ct);
        return audit.Id;
    }

    public async Task RecordCountAsync(long auditId, AddAuditCountRequest request, CancellationToken ct = default)
    {
        var audit = await db.StockAudits.Include(a => a.Items).FirstOrDefaultAsync(a => a.Id == auditId, ct)
            ?? throw new NotFoundException(nameof(StockAudit), auditId);
        if (audit.Status != StockAuditStatus.InProgress)
        {
            throw new DomainException($"An audit that is {audit.Status} can no longer be edited.");
        }
        var item = audit.Items.FirstOrDefault(i => i.BatchId == request.BatchId)
            ?? throw new NotFoundException(nameof(StockAuditItem), request.BatchId);
        item.CountedQuantity = request.CountedQuantity;
        item.Variance = item.CountedQuantity - item.SystemQuantity;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Posts an adjustment StockLedger row per non-zero variance — mandatory transaction, same declared-vs-system idiom as Billing's DayClose.</summary>
    public async Task CompleteAuditAsync(long id, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var audit = await db.StockAudits.Include(a => a.Items).ThenInclude(i => i.Batch)
                .FirstOrDefaultAsync(a => a.Id == id, token) ?? throw new NotFoundException(nameof(StockAudit), id);
            if (audit.Status != StockAuditStatus.InProgress)
            {
                throw new DomainException($"An audit that is {audit.Status} cannot be completed again.");
            }

            var now = timeProvider.GetUtcNow();
            foreach (var item in audit.Items.Where(i => i.Variance != 0))
            {
                var type = item.Variance > 0 ? StockMovementType.AdjustmentIn : StockMovementType.AdjustmentOut;
                StockLedgerWriter.Write(db, item.Batch!, type, Math.Abs(item.Variance), nameof(StockAudit), audit.Id, now, "Stock audit variance");
            }
            audit.Status = StockAuditStatus.Completed;
            audit.CompletedAt = now;
            await activity.LogAsync(ActivityEvents.StockAuditCompleted, $"Stock audit {audit.AuditNumber} completed", ct: token);
            await db.SaveChangesAsync(token);
        }, ct);

    public async Task<IReadOnlyList<StockAuditListItemDto>> GetAuditsAsync(CancellationToken ct = default) =>
        await db.StockAudits.AsNoTracking().Include(a => a.Store)
            .OrderByDescending(a => a.AuditDate).ThenByDescending(a => a.Id)
            .Select(a => new StockAuditListItemDto(a.Id, a.AuditNumber, a.AuditDate, a.Store!.Name, a.Status))
            .ToListAsync(ct);

    public async Task<StockAuditDetailDto> GetAuditByIdAsync(long id, CancellationToken ct = default)
    {
        var a = await db.StockAudits.AsNoTracking().Include(x => x.Store)
            .Include(x => x.Items).ThenInclude(i => i.Batch).ThenInclude(b => b!.Drug)
            .Include(x => x.Items).ThenInclude(i => i.Batch).ThenInclude(b => b!.Item)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(StockAudit), id);
        return new StockAuditDetailDto(a.Id, a.AuditNumber, a.AuditDate, a.StoreId, a.Store!.Name, a.Status,
            a.Items.Select(i => new StockAuditItemDto(i.Id, i.BatchId, ProductNaming.Of(i.Batch!), i.Batch!.BatchNumber, i.SystemQuantity, i.CountedQuantity, i.Variance)).ToList());
    }

    // ── Alerts / reports ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<NearExpiryBatchDto>> GetNearExpiryAsync(int withinDays, CancellationToken ct = default)
    {
        var today = Today();
        var cutoff = today.AddDays(withinDays);
        return await db.Batches.AsNoTracking()
            .Where(b => b.QuantityOnHand > 0 && b.ExpiryDate != null && b.ExpiryDate <= cutoff)
            .Include(b => b.Drug).Include(b => b.Item).Include(b => b.Store)
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new NearExpiryBatchDto(b.Id, b.Drug != null ? b.Drug.Name : b.Item!.Name, b.Store!.Name, b.BatchNumber,
                b.ExpiryDate!.Value, b.QuantityOnHand, (b.ExpiryDate!.Value.DayNumber - today.DayNumber)))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LowStockProductDto>> GetLowStockAsync(CancellationToken ct = default)
    {
        var totals = await db.Batches.AsNoTracking()
            .GroupBy(b => new { b.DrugId, b.ItemId })
            .Select(g => new { g.Key.DrugId, g.Key.ItemId, Total = g.Sum(b => b.QuantityOnHand) })
            .ToListAsync(ct);
        var drugs = await db.Drugs.AsNoTracking().Where(d => d.ReorderLevel > 0).Select(d => new { d.Id, d.Name, d.ReorderLevel }).ToListAsync(ct);
        var items = await db.Items.AsNoTracking().Where(i => i.ReorderLevel > 0).Select(i => new { i.Id, i.Name, i.ReorderLevel }).ToListAsync(ct);

        var drugRows = drugs.GroupJoin(totals.Where(t => t.DrugId != null), d => d.Id, t => t.DrugId,
                (d, ts) => new { d.Id, d.Name, d.ReorderLevel, Total = ts.Select(t => t.Total).DefaultIfEmpty(0).Sum() })
            .Where(x => x.Total < x.ReorderLevel)
            .Select(x => new LowStockProductDto(x.Id, null, x.Name, x.ReorderLevel, x.Total));

        var itemRows = items.GroupJoin(totals.Where(t => t.ItemId != null), i => i.Id, t => t.ItemId,
                (i, ts) => new { i.Id, i.Name, i.ReorderLevel, Total = ts.Select(t => t.Total).DefaultIfEmpty(0).Sum() })
            .Where(x => x.Total < x.ReorderLevel)
            .Select(x => new LowStockProductDto(null, x.Id, x.Name, x.ReorderLevel, x.Total));

        return drugRows.Concat(itemRows).OrderBy(x => x.ProductName).ToList();
    }

    /// <summary>
    /// Net movement, not just outbound totals — a cancelled sale writes a SaleOut row and
    /// a SaleReturnIn reversal row, and without subtracting the reversal a fully-cancelled
    /// sale would still show up as "consumed." Same reasoning applies to issue returns.
    /// Department-wise: issues carry the requesting Department; OP sale rows have none.
    /// </summary>
    public async Task<IReadOnlyList<ProductConsumptionRowDto>> GetConsumptionReportAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        var rangeStart = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var rangeEnd = new DateTimeOffset(toDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(1);
        var relevantTypes = new[]
        {
            StockMovementType.SaleOut, StockMovementType.SaleReturnIn,
            StockMovementType.IssueOut, StockMovementType.IssueReturnIn,
        };

        var ledgerRows = await db.StockLedgers.AsNoTracking()
            .Where(l => l.MovementAt >= rangeStart && l.MovementAt < rangeEnd && relevantTypes.Contains(l.MovementType))
            .Include(l => l.Batch)
            .Select(l => new { l.Batch!.DrugId, l.Batch.ItemId, l.MovementType, l.Quantity, l.ReferenceType, l.ReferenceId })
            .ToListAsync(ct);

        var issueIds = ledgerRows.Where(l => l.ReferenceType == nameof(Issue)).Select(l => l.ReferenceId).Distinct().ToList();
        var issueDepartmentNames = await db.Issues.AsNoTracking().Include(i => i.Department)
            .Where(i => issueIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.Department != null ? i.Department.Name : null, ct);

        var totals = ledgerRows
            .Select(l => new MovementTotal(
                l.DrugId, l.ItemId,
                l.ReferenceType == nameof(Issue) ? issueDepartmentNames.GetValueOrDefault(l.ReferenceId) : null,
                l.MovementType, l.Quantity))
            .ToList();

        var drugIds = totals.Where(t => t.DrugId != null).Select(t => t.DrugId!.Value).Distinct().ToList();
        var itemIds = totals.Where(t => t.ItemId != null).Select(t => t.ItemId!.Value).Distinct().ToList();
        var drugNames = await db.Drugs.AsNoTracking().Where(d => drugIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name, ct);
        var itemNames = await db.Items.AsNoTracking().Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, i => i.Name, ct);

        return totals.GroupBy(t => new { t.DrugId, t.ItemId, t.DepartmentName })
            .Select(g => new ProductConsumptionRowDto(
                g.Key.DrugId, g.Key.ItemId,
                g.Key.DrugId is { } d ? drugNames.GetValueOrDefault(d, "") : itemNames.GetValueOrDefault(g.Key.ItemId!.Value, ""),
                g.Key.DepartmentName,
                SumType(g, StockMovementType.SaleOut) - SumType(g, StockMovementType.SaleReturnIn),
                SumType(g, StockMovementType.IssueOut) - SumType(g, StockMovementType.IssueReturnIn)))
            .Where(r => r.QuantitySold != 0 || r.QuantityIssued != 0)
            .OrderBy(r => r.ProductName)
            .ToList();
    }

    private record MovementTotal(long? DrugId, long? ItemId, string? DepartmentName, StockMovementType MovementType, decimal Total);

    private static decimal SumType(IEnumerable<MovementTotal> rows, StockMovementType type) =>
        rows.Where(r => r.MovementType == type).Sum(r => r.Total);

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
