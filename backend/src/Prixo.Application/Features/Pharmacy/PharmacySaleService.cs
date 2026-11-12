using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Pharmacy;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Pharmacy;

/// <summary>
/// OP pharmacy POS sale — FEFO batch allocation, cancel with stock reversal, sales
/// return. Sale creation and cancellation are mandatory transactions (CLAUDE.md).
/// </summary>
public class PharmacySaleService(
    IAppDbContext db, IUnitOfWork unitOfWork, ISequenceService sequences,
    ICurrentUser currentUser, IActivityLogger activity, IPdfService pdf, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<BatchAvailabilityDto>> GetBatchAvailabilityAsync(
        long drugId, long storeId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return await db.Batches.AsNoTracking()
            .Where(b => b.DrugId == drugId && b.StoreId == storeId && b.QuantityOnHand > 0 && (b.ExpiryDate == null || b.ExpiryDate > today))
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new BatchAvailabilityDto(b.Id, b.BatchNumber, b.ExpiryDate!.Value, b.Mrp!.Value, b.QuantityOnHand))
            .ToListAsync(ct);
    }

    public async Task<PharmacySaleDetailDto> CreateSaleAsync(CreateSaleRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var branchId = RequireBranch();
            if (!await db.Stores.AnyAsync(s => s.Id == request.StoreId, token))
            {
                throw new NotFoundException(nameof(Domain.Entities.Pharmacy.Store), request.StoreId);
            }
            if (request.Items.Count == 0)
            {
                throw new DomainException("Add at least one drug to the sale.");
            }
            if (request.PaymentModeId is { } modeId && !await db.PaymentModes.AnyAsync(m => m.Id == modeId, token))
            {
                throw new NotFoundException(nameof(PaymentMode), modeId);
            }
            if (request.PrescriptionId is { } rxId)
            {
                var rxPatientId = await db.Prescriptions.Where(r => r.Id == rxId).Select(r => (long?)r.Visit!.PatientId).FirstOrDefaultAsync(token)
                    ?? throw new NotFoundException(nameof(Domain.Entities.Opd.Prescription), rxId);
                if (request.PatientId != rxPatientId)
                {
                    throw new DomainException("This prescription belongs to a different patient and cannot be dispensed to the selected patient.");
                }
            }

            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            var number = await sequences.NextValueAsync(SequenceKeys.PharmacySaleNumber, branchId, today.Year, token);
            var sale = new PharmacySale
            {
                BranchId = branchId,
                StoreId = request.StoreId,
                PatientId = request.PatientId,
                PrescriptionId = request.PrescriptionId,
                PaymentModeId = request.PaymentModeId,
                SaleNumber = $"PSALE-{today.Year}-{number.ToString().PadLeft(6, '0')}",
                SaleDate = today,
            };
            db.PharmacySales.Add(sale);
            await db.SaveChangesAsync(token);

            var now = timeProvider.GetUtcNow();
            foreach (var line in request.Items)
            {
                var drug = await db.Drugs.Include(d => d.Tax).FirstOrDefaultAsync(d => d.Id == line.DrugId, token)
                    ?? throw new NotFoundException(nameof(Drug), line.DrugId);

                // Expired batches are never dispensed. Lock the candidate rows (in id order, to avoid
                // deadlocks) so two counters cannot both sell the same last units.
                var candidateIds = await db.Batches.AsNoTracking()
                    .Where(b => b.DrugId == line.DrugId && b.StoreId == request.StoreId && b.QuantityOnHand > 0 && (b.ExpiryDate == null || b.ExpiryDate > today))
                    .OrderBy(b => b.Id).Select(b => b.Id).ToListAsync(token);
                foreach (var batchId in candidateIds)
                {
                    await unitOfWork.LockAsync<Batch>(batchId, token);
                }
                var batches = await db.Batches.Where(b => candidateIds.Contains(b.Id) && b.QuantityOnHand > 0)
                    .ToListAsync(token);
                var candidates = batches.Select(b => new FefoBatchCandidate(b.Id, b.ExpiryDate!.Value, b.QuantityOnHand));
                var (allocations, remaining) = StockMath.AllocateFefo(candidates, line.Quantity);
                if (remaining > 0)
                {
                    throw new DomainException($"Insufficient stock for {drug.Name} — short by {remaining}.");
                }

                var taxPercent = drug.TaxId is not null ? drug.Tax?.RatePercent ?? 0 : 0;
                foreach (var allocation in allocations)
                {
                    var batch = batches.First(b => b.Id == allocation.BatchId);
                    var (discountAmount, taxAmount, totalAmount) = StockMath.ComputeMrpInclusiveLineAmounts(
                        batch.Mrp!.Value, allocation.Quantity, line.DiscountPercent, taxPercent);

                    sale.Items.Add(new PharmacySaleItem
                    {
                        BatchId = batch.Id,
                        Quantity = allocation.Quantity,
                        Rate = batch.Mrp!.Value,
                        DiscountPercent = line.DiscountPercent,
                        DiscountAmount = discountAmount,
                        TaxPercent = taxPercent,
                        TaxAmount = taxAmount,
                        TotalAmount = totalAmount,
                    });
                    StockLedgerWriter.Write(db, batch, StockMovementType.SaleOut, allocation.Quantity, nameof(PharmacySale), sale.Id, now);
                }
            }

            sale.SubTotal = sale.Items.Sum(i => i.Rate * i.Quantity);
            sale.DiscountAmount = sale.Items.Sum(i => i.DiscountAmount);
            sale.TaxAmount = sale.Items.Sum(i => i.TaxAmount);
            sale.TotalAmount = sale.Items.Sum(i => i.TotalAmount);

            await db.SaveChangesAsync(token);
            return await GetByIdAsync(sale.Id, token);
        }, ct);

    public async Task CancelSaleAsync(long id, CancelSaleRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await unitOfWork.LockAsync<PharmacySale>(id, token);
            var sale = await db.PharmacySales.Include(s => s.Items).ThenInclude(i => i.Batch)
                .FirstOrDefaultAsync(s => s.Id == id, token) ?? throw new NotFoundException(nameof(PharmacySale), id);
            if (sale.Status == PharmacySaleStatus.Cancelled)
            {
                throw new DomainException("This sale is already cancelled.");
            }
            if (await db.SalesReturns.AnyAsync(r => r.PharmacySaleId == id, token))
            {
                throw new DomainException("This sale already has returns posted; cancel is not allowed - return the remaining quantity instead.");
            }

            var now = timeProvider.GetUtcNow();
            foreach (var item in sale.Items)
            {
                StockLedgerWriter.Write(db, item.Batch!, StockMovementType.SaleReturnIn, item.Quantity, nameof(PharmacySale), sale.Id, now,
                    "Sale cancelled");
            }
            sale.Status = PharmacySaleStatus.Cancelled;
            sale.CancelReason = request.Reason;
            sale.CancelledAt = now;

            await activity.LogAsync(ActivityEvents.PharmacySaleCancelled, $"Sale {sale.SaleNumber} cancelled: {request.Reason}", ct: token);
            await db.SaveChangesAsync(token);
        }, ct);

    public async Task<PagedResult<PharmacySaleListItemDto>> GetPagedAsync(PharmacySaleQuery query, CancellationToken ct = default)
    {
        var sales = db.PharmacySales.AsNoTracking();
        if (query.FromDate is { } from) sales = sales.Where(s => s.SaleDate >= from);
        if (query.ToDate is { } to) sales = sales.Where(s => s.SaleDate <= to);
        if (query.Status is { } status) sales = sales.Where(s => s.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            sales = sales.Where(s => EF.Functions.ILike(s.SaleNumber, term));
        }

        var totalCount = await sales.CountAsync(ct);
        var items = await sales
            .OrderByDescending(s => s.SaleDate).ThenByDescending(s => s.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(s => new PharmacySaleListItemDto(
                s.Id, s.SaleNumber, s.SaleDate, s.Patient != null ? s.Patient.FirstName + " " + s.Patient.LastName : null,
                s.Status, s.TotalAmount))
            .ToListAsync(ct);
        return PagedResult<PharmacySaleListItemDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<PharmacySaleDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var s = await db.PharmacySales.AsNoTracking()
            .Include(x => x.Store).Include(x => x.Patient).Include(x => x.PaymentMode)
            .Include(x => x.Items).ThenInclude(i => i.Batch).ThenInclude(b => b!.Drug)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(PharmacySale), id);
        return new PharmacySaleDetailDto(
            s.Id, s.SaleNumber, s.SaleDate, s.StoreId, s.Store!.Name, s.PatientId,
            s.Patient != null ? s.Patient.FirstName + " " + s.Patient.LastName : null, s.Status,
            s.SubTotal, s.DiscountAmount, s.TaxAmount, s.TotalAmount,
            s.PaymentModeId, s.PaymentMode?.Name, s.CancelReason,
            s.Items.Select(i => new SaleItemDto(i.Id, i.BatchId, i.Batch!.Drug!.Name, i.Batch.BatchNumber, i.Batch.ExpiryDate!.Value,
                i.Quantity, i.Rate, i.DiscountPercent, i.DiscountAmount, i.TaxPercent, i.TaxAmount, i.TotalAmount)).ToList());
    }

    /// <summary>"Pharmacy Pay Type Modify" — a plain permission-gated correction, not a financial
    /// transaction (no stock/ledger effect, unlike cancel/return), so it's logged via
    /// IActivityLogger but doesn't need IUnitOfWork's transactional guarantee.</summary>
    public async Task ModifyPayTypeAsync(long id, ModifyPayTypeRequest request, CancellationToken ct = default)
    {
        var sale = await db.PharmacySales.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException(nameof(PharmacySale), id);
        if (sale.Status != PharmacySaleStatus.Completed)
        {
            throw new DomainException($"A sale that is {sale.Status} cannot have its payment mode changed.");
        }
        if (request.PaymentModeId is { } paymentModeId && !await db.PaymentModes.AnyAsync(m => m.Id == paymentModeId, ct))
        {
            throw new NotFoundException(nameof(PaymentMode), paymentModeId);
        }

        sale.PaymentModeId = request.PaymentModeId;
        await activity.LogAsync(ActivityEvents.PharmacySalePayTypeModified, $"Payment mode changed for sale {sale.SaleNumber}", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<long> CreateSalesReturnAsync(CreateSalesReturnRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var branchId = RequireBranch();
            await unitOfWork.LockAsync<PharmacySale>(request.PharmacySaleId, token);
            var sale = await db.PharmacySales.Include(s => s.Items).ThenInclude(i => i.Batch)
                .FirstOrDefaultAsync(s => s.Id == request.PharmacySaleId, token)
                ?? throw new NotFoundException(nameof(PharmacySale), request.PharmacySaleId);
            if (sale.Status != PharmacySaleStatus.Completed)
            {
                throw new DomainException($"A sale that is {sale.Status} cannot be returned against.");
            }

            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            var number = await sequences.NextValueAsync(SequenceKeys.SalesReturnNumber, branchId, today.Year, token);
            var salesReturn = new SalesReturn
            {
                BranchId = branchId,
                PharmacySaleId = sale.Id,
                ReturnNumber = $"SRET-{today.Year}-{number.ToString().PadLeft(6, '0')}",
                ReturnDate = today,
                Reason = request.Reason,
            };
            db.SalesReturns.Add(salesReturn);
            await db.SaveChangesAsync(token);

            var now = timeProvider.GetUtcNow();
            decimal total = 0;
            foreach (var line in request.Items)
            {
                var saleItem = sale.Items.FirstOrDefault(i => i.Id == line.PharmacySaleItemId)
                    ?? throw new NotFoundException(nameof(PharmacySaleItem), line.PharmacySaleItemId);
                if (line.Quantity <= 0)
                {
                    throw new DomainException("Return quantity must be greater than zero.");
                }
                var alreadyReturned = await db.SalesReturnItems.Where(r => r.PharmacySaleItemId == saleItem.Id)
                    .SumAsync(r => (decimal?)r.Quantity, token) ?? 0;
                if (alreadyReturned + line.Quantity > saleItem.Quantity)
                {
                    throw new DomainException(
                        $"Return quantity exceeds what is still returnable for this line ({saleItem.Quantity - alreadyReturned} of {saleItem.Quantity} sold).");
                }
                var unitAmount = saleItem.TotalAmount / saleItem.Quantity;
                var amount = Math.Round(unitAmount * line.Quantity, 2);
                salesReturn.Items.Add(new SalesReturnItem { PharmacySaleItemId = saleItem.Id, Quantity = line.Quantity, Amount = amount });
                total += amount;
                StockLedgerWriter.Write(db, saleItem.Batch!, StockMovementType.SaleReturnIn, line.Quantity, nameof(SalesReturn), salesReturn.Id, now);
            }
            salesReturn.TotalAmount = total;

            await db.SaveChangesAsync(token);
            return salesReturn.Id;
        }, ct);

    public async Task<IReadOnlyList<SalesReturnListItemDto>> GetSalesReturnsAsync(CancellationToken ct = default) =>
        await db.SalesReturns.AsNoTracking()
            .OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.Id)
            .Select(r => new SalesReturnListItemDto(r.Id, r.ReturnNumber, r.ReturnDate, r.PharmacySale!.SaleNumber, r.TotalAmount))
            .ToListAsync(ct);

    public async Task<byte[]> GenerateInvoiceAsync(long id, CancellationToken ct = default)
    {
        var s = await db.PharmacySales.AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Items).ThenInclude(i => i.Batch).ThenInclude(b => b!.Drug)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(PharmacySale), id);
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == s.BranchId, ct);

        var data = new PharmacyInvoiceData(
            s.SaleNumber, s.SaleDate, s.Patient != null ? s.Patient.FirstName + " " + s.Patient.LastName : null,
            branch?.Name ?? string.Empty, branch?.Address ?? string.Empty,
            s.Items.Select(i => new PharmacyInvoiceLine(i.Batch!.Drug!.Name, i.Batch.BatchNumber, i.Batch.ExpiryDate!.Value,
                i.Quantity, i.Rate, i.DiscountAmount, i.TaxAmount, i.TotalAmount)).ToList(),
            s.SubTotal, s.DiscountAmount, s.TaxAmount, s.TotalAmount);
        return pdf.GeneratePharmacyInvoice(data);
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
