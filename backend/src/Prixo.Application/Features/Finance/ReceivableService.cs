using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Finance;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Finance;

/// <summary>
/// Receivables (plan §18: "credit/corporate outstanding") — the AR mirror of Payable. A
/// credit bill's own Bill.BalanceAmount is untouched here (Billing never collects a corporate
/// bill at the counter, per Phase 6 — MakeCreditBillAsync only fires on a zero-payment Open
/// bill); Receivable.CollectedAmount is Finance's own sub-ledger tracking of what the
/// corporate has actually settled, independent of Billing.
/// </summary>
public class ReceivableService(IAppDbContext db, IUnitOfWork unitOfWork, VoucherService vouchers, IActivityLogger activity)
{
    /// <summary>
    /// Credit bills with no receivable raised yet. Deliberately excludes a credit bill with no
    /// `CorporateId` (Billing's "Mark as credit" allows marking a bill credit without picking a
    /// corporate — a genuinely common state: ~958 rows in the demo dataset) since `CreateAsync`
    /// below refuses to raise a receivable against one anyway ("Only a credit bill with a
    /// corporate assigned..."). Before this filter, `b.CorporateId!.Value` blew up with
    /// "Nullable object must have a value" the moment the query reached the first such row —
    /// 500ing this entire tab on every call, found during pre-go-live QA.
    /// </summary>
    public async Task<IReadOnlyList<PendingCreditBillDto>> GetPendingBillsAsync(CancellationToken ct = default) =>
        await db.Bills.AsNoTracking()
            .Where(b => b.IsCreditBill && b.CorporateId != null && b.Status != BillStatus.Cancelled
                && !db.Receivables.Any(r => r.BillId == b.Id))
            .Include(b => b.Corporate)
            .OrderByDescending(b => b.BillDate)
            .Select(b => new PendingCreditBillDto(b.Id, b.BillNumber, b.CorporateId!.Value, b.Corporate!.Name, b.BillDate, b.TotalAmount))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(CreateReceivableRequest request, CancellationToken ct = default)
    {
        var bill = await db.Bills.FirstOrDefaultAsync(b => b.Id == request.BillId, ct) ?? throw new NotFoundException(nameof(Bill), request.BillId);
        if (!bill.IsCreditBill || bill.CorporateId is null)
        {
            throw new DomainException("Only a credit bill with a corporate assigned can be raised as a receivable.");
        }
        if (await db.Receivables.AnyAsync(r => r.BillId == request.BillId, ct))
        {
            throw new DomainException("This bill already has a receivable.");
        }

        var receivable = new Receivable
        {
            BranchId = bill.BranchId,
            CorporateId = bill.CorporateId.Value,
            BillId = bill.Id,
            BillDate = bill.BillDate,
            BillAmount = bill.TotalAmount,
            BalanceAmount = bill.TotalAmount,
        };
        db.Receivables.Add(receivable);
        await db.SaveChangesAsync(ct);
        return receivable.Id;
    }

    public async Task RecordCollectionAsync(long id, RecordReceivableCollectionRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var receivable = await db.Receivables.Include(r => r.Corporate).Include(r => r.Bill).FirstOrDefaultAsync(r => r.Id == id, token)
                ?? throw new NotFoundException(nameof(Receivable), id);
            if (receivable.Status == ReceivableStatus.Collected)
            {
                throw new DomainException("This receivable is already fully collected.");
            }
            if (request.Amount > receivable.BalanceAmount)
            {
                throw new DomainException("Collection amount exceeds the outstanding balance.");
            }

            await vouchers.PostReceivableCollectionAsync(receivable.BranchId, receivable.Id, request.CashAccountId, request.Amount, request.Date,
                $"Collection from {receivable.Corporate!.Name} for bill {receivable.Bill!.BillNumber}", token);

            receivable.CollectedAmount += request.Amount;
            (receivable.BalanceAmount, receivable.Status) = FinanceMath.RecomputeReceivable(receivable.BillAmount, receivable.CollectedAmount);

            await activity.LogAsync(ActivityEvents.FinanceReceivableCollected,
                $"Collection of {request.Amount} recorded against receivable for bill {receivable.Bill.BillNumber}", ct: token);
            await db.SaveChangesAsync(token);
        }, ct);

    public async Task<IReadOnlyList<ReceivableDto>> GetAllAsync(ReceivableQuery query, CancellationToken ct = default)
    {
        var receivables = db.Receivables.AsNoTracking().Include(r => r.Corporate).Include(r => r.Bill).AsQueryable();
        if (query.Status is { } status) receivables = receivables.Where(r => r.Status == status);

        return await receivables.OrderByDescending(r => r.BillDate)
            .Select(r => new ReceivableDto(r.Id, r.CorporateId, r.Corporate!.Name, r.BillId, r.Bill!.BillNumber, r.BillDate,
                r.BillAmount, r.CollectedAmount, r.BalanceAmount, r.Status))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AgingRowDto>> GetAgingAsync(DateOnly asOf, CancellationToken ct = default)
    {
        var open = await db.Receivables.AsNoTracking().Include(r => r.Corporate)
            .Where(r => r.Status != ReceivableStatus.Collected)
            .Select(r => new { r.Corporate!.Name, r.BillDate, r.BalanceAmount })
            .ToListAsync(ct);
        return open.Select(r => new AgingRowDto(r.Name, FinanceMath.AgingBucket(asOf, r.BillDate), r.BalanceAmount))
            .GroupBy(r => (r.EntityName, r.Bucket))
            .Select(g => new AgingRowDto(g.Key.EntityName, g.Key.Bucket, g.Sum(r => r.BalanceAmount)))
            .ToList();
    }
}
