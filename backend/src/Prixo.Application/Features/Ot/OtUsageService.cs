using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Billing;
using Prixo.Application.Features.Pharmacy;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Ot;
using Prixo.Domain.Entities.Pharmacy;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Ot;

/// <summary>
/// Batch/serial-tracked implant and general consumable usage — recording either one both
/// decrements the batch's stock (an IssueOut StockLedger row referencing the usage row
/// itself, same "document is its own reference" idiom as GRN/Sale/Adjustment) and posts a
/// BillItem to the admission's open IP bill, in one mandatory transaction (CLAUDE.md:
/// "pharmacy sale/issue" + "billing" both apply here). Rate snapshots the batch's Mrp
/// (falling back to PurchaseRate for consumables that don't carry an Mrp — Phase 13).
/// </summary>
public class OtUsageService(IAppDbContext db, IUnitOfWork unitOfWork, ISequenceService sequences, TimeProvider timeProvider)
{
    public async Task<long> RecordImplantUsageAsync(long bookingId, RecordImplantUsageRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var (_, batch, bill) = await LoadContextAsync(bookingId, request.BatchId, request.Quantity, token);
            var rate = batch.Mrp ?? batch.PurchaseRate;

            var usage = new ImplantUsage { BookingId = bookingId, BatchId = batch.Id, SerialNumber = request.SerialNumber, Quantity = request.Quantity, Rate = rate };
            db.ImplantUsages.Add(usage);
            await db.SaveChangesAsync(token);

            var now = timeProvider.GetUtcNow();
            StockLedgerWriter.Write(db, batch, StockMovementType.IssueOut, request.Quantity, nameof(ImplantUsage), usage.Id, now);

            var billItem = new BillItem
            {
                BillId = bill.Id,
                SourceType = BillItemSourceType.OtCharge,
                Description = $"Implant — {ProductNaming.Of(batch)}" + (request.SerialNumber is { } sn ? $" (SN {sn})" : ""),
                Quantity = request.Quantity,
                Rate = rate,
            };
            BillMath.ComputeItemAmounts(billItem);
            bill.Items.Add(billItem);
            BillMath.RecomputeBillTotals(bill);
            await db.SaveChangesAsync(token);

            usage.BillItemId = billItem.Id;
            await db.SaveChangesAsync(token);
            return usage.Id;
        }, ct);

    public async Task<long> RecordConsumableUsageAsync(long bookingId, RecordConsumableUsageRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var (_, batch, bill) = await LoadContextAsync(bookingId, request.BatchId, request.Quantity, token);
            var rate = batch.Mrp ?? batch.PurchaseRate;

            var usage = new ConsumableUsage { BookingId = bookingId, BatchId = batch.Id, Quantity = request.Quantity, Rate = rate };
            db.ConsumableUsages.Add(usage);
            await db.SaveChangesAsync(token);

            var now = timeProvider.GetUtcNow();
            StockLedgerWriter.Write(db, batch, StockMovementType.IssueOut, request.Quantity, nameof(ConsumableUsage), usage.Id, now);

            var billItem = new BillItem
            {
                BillId = bill.Id,
                SourceType = BillItemSourceType.OtCharge,
                Description = $"Consumable — {ProductNaming.Of(batch)}",
                Quantity = request.Quantity,
                Rate = rate,
            };
            BillMath.ComputeItemAmounts(billItem);
            bill.Items.Add(billItem);
            BillMath.RecomputeBillTotals(bill);
            await db.SaveChangesAsync(token);

            usage.BillItemId = billItem.Id;
            await db.SaveChangesAsync(token);
            return usage.Id;
        }, ct);

    public async Task<IReadOnlyList<ImplantUsageDto>> GetImplantUsageAsync(long bookingId, CancellationToken ct = default) =>
        await db.ImplantUsages.AsNoTracking().Where(u => u.BookingId == bookingId)
            .Include(u => u.Batch).ThenInclude(b => b!.Drug)
            .Include(u => u.Batch).ThenInclude(b => b!.Item)
            .Select(u => new ImplantUsageDto(u.Id, u.BookingId, u.BatchId, u.Batch!.Drug != null ? u.Batch.Drug.Name : u.Batch.Item!.Name, u.SerialNumber, u.Quantity, u.Rate))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ConsumableUsageDto>> GetConsumableUsageAsync(long bookingId, CancellationToken ct = default) =>
        await db.ConsumableUsages.AsNoTracking().Where(u => u.BookingId == bookingId)
            .Include(u => u.Batch).ThenInclude(b => b!.Drug)
            .Include(u => u.Batch).ThenInclude(b => b!.Item)
            .Select(u => new ConsumableUsageDto(u.Id, u.BookingId, u.BatchId, u.Batch!.Drug != null ? u.Batch.Drug.Name : u.Batch.Item!.Name, u.Quantity, u.Rate))
            .ToListAsync(ct);

    private async Task<(SurgeryBooking Booking, Batch Batch, Bill Bill)> LoadContextAsync(long bookingId, long batchId, decimal quantity, CancellationToken ct)
    {
        var booking = await db.SurgeryBookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new NotFoundException(nameof(SurgeryBooking), bookingId);
        if (booking.Status != SurgeryStatus.InProgress)
        {
            throw new DomainException("Implant/consumable usage can only be recorded while the surgery is in progress.");
        }

        await unitOfWork.LockAsync<Batch>(batchId, ct);
        var batch = await db.Batches.Include(b => b.Drug).Include(b => b.Item).FirstOrDefaultAsync(b => b.Id == batchId, ct)
            ?? throw new NotFoundException(nameof(Batch), batchId);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        if (!StockMath.IsUsable(batch.ExpiryDate, today))
        {
            throw new DomainException($"Batch {batch.BatchNumber} expired on {batch.ExpiryDate:dd-MMM-yyyy} and cannot be used.");
        }
        if (batch.QuantityOnHand < quantity)
        {
            throw new DomainException($"Batch {batch.BatchNumber} only has {batch.QuantityOnHand} on hand.");
        }

        // Charges post to the admission's running IP bill, created on first use — theatre staff never wait on billing.
        var bill = await IpBillAccess.GetOrCreateOpenAsync(db, sequences, booking.AdmissionId, today, ct);

        return (booking, batch, bill);
    }
}
