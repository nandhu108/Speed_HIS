using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Pharmacy;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Pharmacy;

/// <summary>
/// Purchase Request → Purchase Order → GRN → Purchase Return. Only GRN posting and
/// Purchase Return actually touch stock (CLAUDE.md's mandatory-transaction list); request/
/// order approval is a status gate only.
/// </summary>
public class PurchaseService(
    IAppDbContext db, IUnitOfWork unitOfWork, ISequenceService sequences,
    ICurrentUser currentUser, IActivityLogger activity, TimeProvider timeProvider)
{
    // ── Purchase requests ────────────────────────────────────────────────────

    public async Task<long> CreatePurchaseRequestAsync(CreatePurchaseRequestRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var today = Today();
        var number = await sequences.NextValueAsync(SequenceKeys.PurchaseRequestNumber, branchId, today.Year, ct);

        var purchaseRequest = new PurchaseRequest
        {
            BranchId = branchId,
            RequestNumber = $"PR-{today.Year}-{number.ToString().PadLeft(6, '0')}",
            RequestDate = today,
            Notes = request.Notes,
            Items = request.Items.Select(i => new PurchaseRequestItem { DrugId = i.DrugId, ItemId = i.ItemId, RequestedQuantity = i.RequestedQuantity }).ToList(),
        };
        db.PurchaseRequests.Add(purchaseRequest);
        await db.SaveChangesAsync(ct);
        return purchaseRequest.Id;
    }

    public async Task<PagedResult<PurchaseRequestListItemDto>> GetPurchaseRequestsAsync(PurchaseRequestQuery query, CancellationToken ct = default)
    {
        var requests = db.PurchaseRequests.AsNoTracking();
        if (query.Status is { } status) requests = requests.Where(r => r.Status == status);

        var totalCount = await requests.CountAsync(ct);
        var items = await requests
            .OrderByDescending(r => r.RequestDate).ThenByDescending(r => r.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(r => new PurchaseRequestListItemDto(r.Id, r.RequestNumber, r.RequestDate, r.Status, r.Items.Count))
            .ToListAsync(ct);
        return PagedResult<PurchaseRequestListItemDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<PurchaseRequestDetailDto> GetPurchaseRequestByIdAsync(long id, CancellationToken ct = default)
    {
        var r = await db.PurchaseRequests.AsNoTracking()
            .Include(x => x.Items).ThenInclude(i => i.Drug)
            .Include(x => x.Items).ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(PurchaseRequest), id);
        return new PurchaseRequestDetailDto(r.Id, r.RequestNumber, r.RequestDate, r.Status, r.Notes,
            r.Items.Select(i => new PurchaseRequestItemDto(i.Id, i.DrugId, i.ItemId, i.Drug?.Name ?? i.Item?.Name ?? "", i.RequestedQuantity)).ToList());
    }

    public async Task DecidePurchaseRequestAsync(long id, DecidePurchaseRequestRequest request, CancellationToken ct = default)
    {
        var r = await db.PurchaseRequests.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(PurchaseRequest), id);
        if (r.Status is not (PurchaseRequestStatus.Draft or PurchaseRequestStatus.Submitted))
        {
            throw new DomainException($"A purchase request that is {r.Status} cannot be decided again.");
        }
        r.Status = request.Approve ? PurchaseRequestStatus.Approved : PurchaseRequestStatus.Rejected;
        await db.SaveChangesAsync(ct);
    }

    // ── Purchase orders ──────────────────────────────────────────────────────

    public async Task<long> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        if (!await db.Vendors.AnyAsync(v => v.Id == request.VendorId, ct))
        {
            throw new NotFoundException(nameof(Vendor), request.VendorId);
        }
        var today = Today();
        var number = await sequences.NextValueAsync(SequenceKeys.PurchaseOrderNumber, branchId, today.Year, ct);

        var order = new PurchaseOrder
        {
            BranchId = branchId,
            VendorId = request.VendorId,
            PurchaseRequestId = request.PurchaseRequestId,
            PoNumber = $"PO-{today.Year}-{number.ToString().PadLeft(6, '0')}",
            OrderDate = today,
            Notes = request.Notes,
            Items = request.Items.Select(i => new PurchaseOrderItem
            {
                DrugId = i.DrugId,
                ItemId = i.ItemId,
                OrderedQuantity = i.OrderedQuantity,
                Rate = i.Rate,
                TaxPercent = i.TaxPercent,
            }).ToList(),
        };
        db.PurchaseOrders.Add(order);

        if (request.PurchaseRequestId is { } prId)
        {
            var pr = await db.PurchaseRequests.FirstOrDefaultAsync(x => x.Id == prId, ct);
            if (pr is not null) pr.Status = PurchaseRequestStatus.Converted;
        }

        await db.SaveChangesAsync(ct);
        return order.Id;
    }

    public async Task<PagedResult<PurchaseOrderListItemDto>> GetPurchaseOrdersAsync(PurchaseOrderQuery query, CancellationToken ct = default)
    {
        var orders = db.PurchaseOrders.AsNoTracking();
        if (query.Status is { } status) orders = orders.Where(o => o.Status == status);

        var totalCount = await orders.CountAsync(ct);
        var items = await orders
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(o => new PurchaseOrderListItemDto(
                o.Id, o.PoNumber, o.OrderDate, o.VendorId, o.Vendor!.Name, o.Status,
                o.Items.Sum(i => i.OrderedQuantity * i.Rate * (1 + i.TaxPercent / 100))))
            .ToListAsync(ct);
        return PagedResult<PurchaseOrderListItemDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<PurchaseOrderDetailDto> GetPurchaseOrderByIdAsync(long id, CancellationToken ct = default)
    {
        var o = await db.PurchaseOrders.AsNoTracking().Include(x => x.Vendor)
            .Include(x => x.Items).ThenInclude(i => i.Drug)
            .Include(x => x.Items).ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(PurchaseOrder), id);
        return new PurchaseOrderDetailDto(o.Id, o.PoNumber, o.OrderDate, o.VendorId, o.Vendor!.Name, o.PurchaseRequestId,
            o.Status, o.Notes,
            o.Items.Select(i => new PurchaseOrderItemDto(i.Id, i.DrugId, i.ItemId, i.Drug?.Name ?? i.Item?.Name ?? "",
                i.OrderedQuantity, i.ReceivedQuantity, i.Rate, i.TaxPercent)).ToList());
    }

    public async Task ApprovePurchaseOrderAsync(long id, CancellationToken ct = default)
    {
        var o = await db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(PurchaseOrder), id);
        if (o.Status != PurchaseOrderStatus.Draft)
        {
            throw new DomainException($"A purchase order that is {o.Status} cannot be approved.");
        }
        o.Status = PurchaseOrderStatus.Approved;
        await db.SaveChangesAsync(ct);
    }

    // ── GRN ──────────────────────────────────────────────────────────────────

    public async Task<long> CreateGrnAsync(CreateGrnRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        if (!await db.Vendors.AnyAsync(v => v.Id == request.VendorId, ct))
        {
            throw new NotFoundException(nameof(Vendor), request.VendorId);
        }
        if (!await db.Stores.AnyAsync(s => s.Id == request.StoreId, ct))
        {
            throw new NotFoundException(nameof(Domain.Entities.Pharmacy.Store), request.StoreId);
        }

        var today = Today();
        if (request.Items.Count == 0)
        {
            throw new DomainException("Add at least one line to the GRN.");
        }
        foreach (var line in request.Items)
        {
            if (line.ReceivedQuantity <= 0 || line.FreeQuantity < 0)
            {
                throw new DomainException($"Batch {line.BatchNumber}: received quantity must be greater than zero and free quantity cannot be negative.");
            }
            if (line.DrugId is not null)
            {
                // Medicines must carry an expiry, and an already-expired (or expiring today) batch is
                // never accepted into saleable stock.
                if (line.ExpiryDate is not { } expiry)
                {
                    throw new DomainException($"Batch {line.BatchNumber}: expiry date is required for medicines.");
                }
                if (expiry <= today)
                {
                    throw new DomainException($"Batch {line.BatchNumber} expired on {expiry:dd-MMM-yyyy} and cannot be received into stock.");
                }
                if (line.Mrp is { } mrp && mrp < line.PurchaseRate)
                {
                    throw new DomainException($"Batch {line.BatchNumber}: MRP ({mrp:0.00}) is below the purchase rate ({line.PurchaseRate:0.00}).");
                }
            }
        }
        if (request.PurchaseOrderId is { } poId)
        {
            var poLines = await db.PurchaseOrderItems.AsNoTracking().Where(i => i.PurchaseOrderId == poId).ToListAsync(ct);
            // Quantity already sitting on other draft GRNs for this PO counts as "on its way in".
            var drafts = await db.GrnItems.AsNoTracking()
                .Where(i => i.Grn!.PurchaseOrderId == poId && i.Grn.Status == DocumentStatus.Draft)
                .Select(i => new { i.DrugId, i.ItemId, i.ReceivedQuantity }).ToListAsync(ct);
            foreach (var group in request.Items.GroupBy(i => new { i.DrugId, i.ItemId }))
            {
                var poLine = poLines.FirstOrDefault(l => l.DrugId == group.Key.DrugId && l.ItemId == group.Key.ItemId)
                    ?? throw new DomainException("A GRN line is for a product that is not on the purchase order.");
                var pending = drafts.Where(x => x.DrugId == group.Key.DrugId && x.ItemId == group.Key.ItemId).Sum(x => x.ReceivedQuantity);
                var remaining = poLine.OrderedQuantity - poLine.ReceivedQuantity - pending;
                var receiving = group.Sum(i => i.ReceivedQuantity);
                if (receiving > remaining)
                {
                    throw new DomainException($"Receiving {receiving} exceeds the quantity still due on the purchase order ({Math.Max(remaining, 0)}).");
                }
            }
        }
        var number = await sequences.NextValueAsync(SequenceKeys.GrnNumber, branchId, today.Year, ct);

        var items = request.Items.Select(i =>
        {
            var amount = i.ReceivedQuantity * i.PurchaseRate * (1 + i.TaxPercent / 100);
            return new GrnItem
            {
                DrugId = i.DrugId,
                ItemId = i.ItemId,
                BatchNumber = i.BatchNumber,
                ExpiryDate = i.ExpiryDate,
                Mrp = i.Mrp,
                PurchaseRate = i.PurchaseRate,
                ReceivedQuantity = i.ReceivedQuantity,
                FreeQuantity = i.FreeQuantity,
                TaxPercent = i.TaxPercent,
                Amount = Math.Round(amount, 2),
            };
        }).ToList();

        var grn = new Grn
        {
            BranchId = branchId,
            PurchaseOrderId = request.PurchaseOrderId,
            VendorId = request.VendorId,
            StoreId = request.StoreId,
            GrnNumber = $"GRN-{today.Year}-{number.ToString().PadLeft(6, '0')}",
            ReceivedDate = today,
            SupplierInvoiceNumber = request.SupplierInvoiceNumber,
            SupplierInvoiceDate = request.SupplierInvoiceDate,
            TotalAmount = items.Sum(i => i.Amount),
            Items = items,
        };
        db.Grns.Add(grn);
        await db.SaveChangesAsync(ct);
        return grn.Id;
    }

    /// <summary>Posting creates/tops up a Batch per line and writes a PurchaseIn StockLedger row — mandatory transaction (CLAUDE.md).</summary>
    public async Task<GrnDetailDto> PostGrnAsync(long id, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await unitOfWork.LockAsync<Grn>(id, token);
            var grn = await db.Grns.Include(g => g.Items).FirstOrDefaultAsync(g => g.Id == id, token)
                ?? throw new NotFoundException(nameof(Grn), id);
            if (grn.Status != DocumentStatus.Draft)
            {
                throw new DomainException($"A GRN that is {grn.Status} cannot be posted.");
            }
            var postingDay = Today();
            var expired = grn.Items.FirstOrDefault(i => i.DrugId != null && (i.ExpiryDate is null || i.ExpiryDate <= postingDay));
            if (expired is not null)
            {
                throw new DomainException($"Batch {expired.BatchNumber} has expired (or has no expiry) and cannot be posted into stock.");
            }

            var now = timeProvider.GetUtcNow();
            foreach (var item in grn.Items)
            {
                var batch = await db.Batches.FirstOrDefaultAsync(b =>
                    b.DrugId == item.DrugId && b.ItemId == item.ItemId && b.StoreId == grn.StoreId && b.BatchNumber == item.BatchNumber, token);
                if (batch is null)
                {
                    batch = new Batch
                    {
                        BranchId = grn.BranchId,
                        DrugId = item.DrugId,
                        ItemId = item.ItemId,
                        StoreId = grn.StoreId,
                        BatchNumber = item.BatchNumber,
                        ExpiryDate = item.ExpiryDate,
                        Mrp = item.Mrp,
                        PurchaseRate = item.PurchaseRate,
                    };
                    db.Batches.Add(batch);
                }
                else
                {
                    batch.Mrp = item.Mrp;
                    batch.PurchaseRate = item.PurchaseRate;
                }

                var totalQuantity = item.ReceivedQuantity + item.FreeQuantity;
                StockLedgerWriter.Write(db, batch, StockMovementType.PurchaseIn, totalQuantity, nameof(Grn), grn.Id, now);
                item.Batch = batch;

                if (grn.PurchaseOrderId is { } poId)
                {
                    var poItem = await db.PurchaseOrderItems.FirstOrDefaultAsync(
                        poi => poi.PurchaseOrderId == poId && poi.DrugId == item.DrugId && poi.ItemId == item.ItemId, token);
                    if (poItem is not null) poItem.ReceivedQuantity += item.ReceivedQuantity;
                }
            }

            if (grn.PurchaseOrderId is { } purchaseOrderId)
            {
                var order = await db.PurchaseOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == purchaseOrderId, token);
                if (order is not null)
                {
                    order.Status = order.Items.All(i => i.ReceivedQuantity >= i.OrderedQuantity)
                        ? PurchaseOrderStatus.Received
                        : PurchaseOrderStatus.PartiallyReceived;
                }
            }

            grn.Status = DocumentStatus.Posted;
            await activity.LogAsync(ActivityEvents.GrnPosted, $"GRN {grn.GrnNumber} posted — {grn.Items.Count} line(s).", ct: token);
            await db.SaveChangesAsync(token);
            return await GetGrnByIdAsync(grn.Id, token);
        }, ct);

    public async Task<PagedResult<GrnListItemDto>> GetGrnsAsync(GrnQuery query, CancellationToken ct = default)
    {
        var grns = db.Grns.AsNoTracking();
        if (query.Status is { } status) grns = grns.Where(g => g.Status == status);

        var totalCount = await grns.CountAsync(ct);
        var items = await grns
            .OrderByDescending(g => g.ReceivedDate).ThenByDescending(g => g.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(g => new GrnListItemDto(g.Id, g.GrnNumber, g.ReceivedDate, g.VendorId, g.Vendor!.Name, g.Status, g.TotalAmount))
            .ToListAsync(ct);
        return PagedResult<GrnListItemDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<GrnDetailDto> GetGrnByIdAsync(long id, CancellationToken ct = default)
    {
        var g = await db.Grns.AsNoTracking().Include(x => x.Vendor).Include(x => x.Store)
            .Include(x => x.Items).ThenInclude(i => i.Drug)
            .Include(x => x.Items).ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(Grn), id);
        return new GrnDetailDto(g.Id, g.GrnNumber, g.ReceivedDate, g.PurchaseOrderId, g.VendorId, g.Vendor!.Name,
            g.StoreId, g.Store!.Name, g.SupplierInvoiceNumber, g.SupplierInvoiceDate, g.Status, g.TotalAmount,
            g.Items.Select(i => new GrnItemDto(i.Id, i.DrugId, i.ItemId, i.Drug?.Name ?? i.Item?.Name ?? "", i.BatchNumber, i.ExpiryDate, i.Mrp,
                i.PurchaseRate, i.ReceivedQuantity, i.FreeQuantity, i.TaxPercent, i.Amount)).ToList());
    }

    // ── Purchase returns ─────────────────────────────────────────────────────

    /// <summary>Created and posted in one step — the decision to return has already been made by the time this is called.</summary>
    public async Task<long> CreatePurchaseReturnAsync(CreatePurchaseReturnRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var branchId = RequireBranch();
            if (!await db.Vendors.AnyAsync(v => v.Id == request.VendorId, token))
            {
                throw new NotFoundException(nameof(Vendor), request.VendorId);
            }

            var today = Today();
            var number = await sequences.NextValueAsync(SequenceKeys.PurchaseReturnNumber, branchId, today.Year, token);
            var purchaseReturn = new PurchaseReturn
            {
                BranchId = branchId,
                GrnId = request.GrnId,
                VendorId = request.VendorId,
                ReturnNumber = $"PRET-{today.Year}-{number.ToString().PadLeft(6, '0')}",
                ReturnDate = today,
                Reason = request.Reason,
                Status = DocumentStatus.Posted,
            };
            db.PurchaseReturns.Add(purchaseReturn);
            await db.SaveChangesAsync(token);

            var now = timeProvider.GetUtcNow();
            decimal total = 0;
            foreach (var line in request.Items)
            {
                var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == line.BatchId, token)
                    ?? throw new NotFoundException(nameof(Batch), line.BatchId);
                if (batch.QuantityOnHand < line.Quantity)
                {
                    throw new DomainException($"Batch {batch.BatchNumber} only has {batch.QuantityOnHand} on hand.");
                }
                StockLedgerWriter.Write(db, batch, StockMovementType.PurchaseReturnOut, line.Quantity,
                    nameof(PurchaseReturn), purchaseReturn.Id, now);
                purchaseReturn.Items.Add(new PurchaseReturnItem { BatchId = batch.Id, Quantity = line.Quantity, Rate = line.Rate });
                total += line.Quantity * line.Rate;
            }
            purchaseReturn.TotalAmount = Math.Round(total, 2);

            await activity.LogAsync(ActivityEvents.PurchaseReturnPosted,
                $"Purchase return {purchaseReturn.ReturnNumber} posted: {request.Reason}", ct: token);
            await db.SaveChangesAsync(token);
            return purchaseReturn.Id;
        }, ct);

    public async Task<IReadOnlyList<PurchaseReturnListItemDto>> GetPurchaseReturnsAsync(CancellationToken ct = default) =>
        await db.PurchaseReturns.AsNoTracking()
            .OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.Id)
            .Select(r => new PurchaseReturnListItemDto(r.Id, r.ReturnNumber, r.ReturnDate, r.VendorId, r.Vendor!.Name, r.TotalAmount))
            .ToListAsync(ct);

    public async Task<PurchaseReturnDetailDto> GetPurchaseReturnByIdAsync(long id, CancellationToken ct = default)
    {
        var r = await db.PurchaseReturns.AsNoTracking().Include(x => x.Vendor)
            .Include(x => x.Items).ThenInclude(i => i.Batch).ThenInclude(b => b!.Drug)
            .Include(x => x.Items).ThenInclude(i => i.Batch).ThenInclude(b => b!.Item)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(PurchaseReturn), id);
        return new PurchaseReturnDetailDto(r.Id, r.ReturnNumber, r.ReturnDate, r.GrnId, r.VendorId, r.Vendor!.Name, r.Reason,
            r.TotalAmount,
            r.Items.Select(i => new PurchaseReturnItemDto(i.Id, i.BatchId, ProductNaming.Of(i.Batch!), i.Batch!.BatchNumber, i.Quantity, i.Rate)).ToList());
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
