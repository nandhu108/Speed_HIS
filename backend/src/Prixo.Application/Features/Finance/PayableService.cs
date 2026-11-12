using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Finance;
using Prixo.Domain.Entities.Pharmacy;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Finance;

/// <summary>
/// Payables (plan §18: "supplier invoices from GRN → payment"). Raising a payable from a
/// posted GRN is an explicit Finance action (the pending-GRN worklist), not automatic —
/// Finance decides when a supplier invoice enters the AP workflow. Recording a payment posts
/// a real ledger voucher via VoucherService (Debit Accounts Payable / Credit the chosen
/// bank-cash account).
/// </summary>
public class PayableService(IAppDbContext db, IUnitOfWork unitOfWork, VoucherService vouchers, IActivityLogger activity)
{
    public async Task<IReadOnlyList<PendingGrnDto>> GetPendingGrnsAsync(CancellationToken ct = default) =>
        await db.Grns.AsNoTracking()
            .Where(g => g.Status == DocumentStatus.Posted && !db.Payables.Any(p => p.GrnId == g.Id))
            .Include(g => g.Vendor)
            .OrderByDescending(g => g.ReceivedDate)
            .Select(g => new PendingGrnDto(g.Id, g.GrnNumber, g.VendorId, g.Vendor!.Name, g.ReceivedDate, g.SupplierInvoiceNumber, g.TotalAmount))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(CreatePayableRequest request, CancellationToken ct = default)
    {
        var grn = await db.Grns.FirstOrDefaultAsync(g => g.Id == request.GrnId, ct) ?? throw new NotFoundException(nameof(Grn), request.GrnId);
        if (grn.Status != DocumentStatus.Posted)
        {
            throw new DomainException("Only a posted GRN can be raised as a payable.");
        }
        if (await db.Payables.AnyAsync(p => p.GrnId == request.GrnId, ct))
        {
            throw new DomainException("This GRN already has a payable.");
        }

        var payable = new Payable
        {
            BranchId = grn.BranchId,
            VendorId = grn.VendorId,
            GrnId = grn.Id,
            InvoiceNumber = grn.SupplierInvoiceNumber ?? grn.GrnNumber,
            InvoiceDate = grn.SupplierInvoiceDate ?? grn.ReceivedDate,
            InvoiceAmount = grn.TotalAmount,
            BalanceAmount = grn.TotalAmount,
            DueDate = request.DueDate,
        };
        db.Payables.Add(payable);
        await db.SaveChangesAsync(ct);
        return payable.Id;
    }

    public async Task RecordPaymentAsync(long id, RecordPayablePaymentRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var payable = await db.Payables.Include(p => p.Vendor).FirstOrDefaultAsync(p => p.Id == id, token)
                ?? throw new NotFoundException(nameof(Payable), id);
            if (payable.Status == PayableStatus.Paid)
            {
                throw new DomainException("This payable is already fully paid.");
            }
            if (request.Amount > payable.BalanceAmount)
            {
                throw new DomainException("Payment amount exceeds the outstanding balance.");
            }

            await vouchers.PostPayableSettlementAsync(payable.BranchId, payable.Id, request.CashAccountId, request.Amount, request.Date,
                $"Payment to {payable.Vendor!.Name} for invoice {payable.InvoiceNumber}", token);

            payable.PaidAmount += request.Amount;
            (payable.BalanceAmount, payable.Status) = FinanceMath.RecomputePayable(payable.InvoiceAmount, payable.PaidAmount);

            await activity.LogAsync(ActivityEvents.FinancePayableSettled,
                $"Payment of {request.Amount} recorded against payable for invoice {payable.InvoiceNumber}", ct: token);
            await db.SaveChangesAsync(token);
        }, ct);

    public async Task<IReadOnlyList<PayableDto>> GetAllAsync(PayableQuery query, CancellationToken ct = default)
    {
        var payables = db.Payables.AsNoTracking().Include(p => p.Vendor).Include(p => p.Grn).AsQueryable();
        if (query.Status is { } status) payables = payables.Where(p => p.Status == status);

        return await payables.OrderByDescending(p => p.InvoiceDate)
            .Select(p => new PayableDto(p.Id, p.VendorId, p.Vendor!.Name, p.GrnId, p.Grn!.GrnNumber, p.InvoiceNumber, p.InvoiceDate,
                p.InvoiceAmount, p.PaidAmount, p.BalanceAmount, p.DueDate, p.Status))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AgingRowDto>> GetAgingAsync(DateOnly asOf, CancellationToken ct = default)
    {
        var open = await db.Payables.AsNoTracking().Include(p => p.Vendor)
            .Where(p => p.Status != PayableStatus.Paid)
            .Select(p => new { p.Vendor!.Name, ReferenceDate = p.DueDate ?? p.InvoiceDate, p.BalanceAmount })
            .ToListAsync(ct);
        return open.Select(p => new AgingRowDto(p.Name, FinanceMath.AgingBucket(asOf, p.ReferenceDate), p.BalanceAmount))
            .GroupBy(r => (r.EntityName, r.Bucket))
            .Select(g => new AgingRowDto(g.Key.EntityName, g.Key.Bucket, g.Sum(r => r.BalanceAmount)))
            .ToList();
    }
}
