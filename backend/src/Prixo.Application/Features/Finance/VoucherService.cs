using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Finance;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Finance;

/// <summary>
/// The single posting engine (plan §18: Journal/Payment/Receipt vouchers, and "billing/
/// pharmacy/GRN events post to ledger via internal event handlers"). Every ledger impact in
/// the system — manual voucher entry, Payable/Receivable settlement, or an automatic event —
/// funnels through <see cref="PostCoreAsync"/> so double-entry balance and the Voucher→
/// VoucherLine→LedgerEntry write pattern only exist once. Implements
/// <see cref="IFinanceLedgerPoster"/> so BillingService can post without depending on this
/// concrete class.
/// </summary>
public class VoucherService(
    IAppDbContext db, IUnitOfWork unitOfWork, ISequenceService sequences, ICurrentUser currentUser,
    IActivityLogger activity, TimeProvider timeProvider)
    : IFinanceLedgerPoster
{
    public async Task<VoucherDto> PostAsync(PostVoucherRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var accountIds = request.Lines.Select(l => l.AccountId).Distinct().ToList();
        var existingCount = await db.Accounts.CountAsync(a => accountIds.Contains(a.Id), ct);
        if (existingCount != accountIds.Count)
        {
            throw new DomainException("One or more accounts on this voucher do not exist.");
        }
        if (request.Lines.Any(l => l.DebitAmount > 0 && l.CreditAmount > 0))
        {
            throw new DomainException("A voucher line cannot have both a debit and a credit amount.");
        }
        if (!FinanceMath.IsBalanced(request.Lines.Select(l => (l.DebitAmount, l.CreditAmount)).ToList()))
        {
            throw new DomainException("Total debits must equal total credits.");
        }

        var voucherId = await PostCoreAsync(
            branchId, request.VoucherType, request.VoucherDate, request.Narration,
            request.Lines.Select(l => (l.AccountId, l.DebitAmount, l.CreditAmount, l.Narration)).ToList(),
            sourceReference: null, ct);
        return await GetByIdAsync(voucherId, ct);
    }

    public async Task CancelAsync(long id, CancelVoucherRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var voucher = await db.Vouchers.Include(v => v.Lines).FirstOrDefaultAsync(v => v.Id == id, token)
                ?? throw new NotFoundException(nameof(Voucher), id);
            if (voucher.Status == VoucherStatus.Cancelled)
            {
                throw new DomainException("This voucher is already cancelled.");
            }

            var reversalLines = voucher.Lines
                .Select(l => (l.AccountId, DebitAmount: l.CreditAmount, CreditAmount: l.DebitAmount, Narration: (string?)$"Reversal of {voucher.VoucherNumber}"))
                .ToList();
            await PostCoreAsync(voucher.BranchId, voucher.VoucherType, DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
                $"Reversal of {voucher.VoucherNumber}: {request.Reason}", reversalLines, $"REVERSAL:{voucher.Id}", token, reversalOfVoucherId: voucher.Id);

            voucher.Status = VoucherStatus.Cancelled;
            voucher.CancelledAt = timeProvider.GetUtcNow();
            voucher.CancelReason = request.Reason;
            await activity.LogAsync(ActivityEvents.FinanceVoucherCancelled, $"Voucher {voucher.VoucherNumber} cancelled: {request.Reason}", ct: token);
            await db.SaveChangesAsync(token);
        }, ct);

    public async Task<IReadOnlyList<VoucherDto>> GetListAsync(VoucherQuery query, CancellationToken ct = default)
    {
        var vouchers = db.Vouchers.AsNoTracking().Include(v => v.Lines).ThenInclude(l => l.Account).Include(v => v.PostedByUser).AsQueryable();
        if (query.VoucherType is { } type) vouchers = vouchers.Where(v => v.VoucherType == type);
        if (query.Status is { } status) vouchers = vouchers.Where(v => v.Status == status);
        if (query.FromDate is { } from) vouchers = vouchers.Where(v => v.VoucherDate >= from);
        if (query.ToDate is { } to) vouchers = vouchers.Where(v => v.VoucherDate <= to);

        var rows = await vouchers.OrderByDescending(v => v.VoucherDate).ThenByDescending(v => v.Id).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<VoucherDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var v = await db.Vouchers.AsNoTracking().Include(x => x.Lines).ThenInclude(l => l.Account).Include(x => x.PostedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(Voucher), id);
        return ToDto(v);
    }

    // ── IFinanceLedgerPoster ─────────────────────────────────────────────────
    public async Task PostBillPaymentAsync(long billId, string billNumber, long paymentModeId, decimal amount, DateOnly date, CancellationToken ct = default)
    {
        var mode = await db.PaymentModes.AsNoTracking().FirstOrDefaultAsync(m => m.Id == paymentModeId, ct);
        if (mode?.LinkedAccountId is not { } bankAccountId)
        {
            return; // This payment mode has no Finance account configured yet — nothing to post.
        }
        var revenueAccountId = await db.Accounts.AsNoTracking()
            .Where(a => a.Code == SystemAccountCodes.PatientRevenue).Select(a => (long?)a.Id).FirstOrDefaultAsync(ct);
        if (revenueAccountId is null)
        {
            return; // System accounts not seeded for this environment yet.
        }

        var billBranchId = await db.Bills.AsNoTracking().Where(b => b.Id == billId).Select(b => b.BranchId).FirstOrDefaultAsync(ct);
        await PostCoreAsync(billBranchId, VoucherType.Receipt, date, $"Payment received for bill {billNumber}",
            [(bankAccountId, amount, 0m, (string?)null), (revenueAccountId.Value, 0m, amount, null)],
            $"BILL_PAYMENT:{billId}", ct);
    }

    public async Task PostCreditNoteAsync(long billId, string billNumber, string creditNoteNumber, decimal amount, DateOnly date, CancellationToken ct = default)
    {
        var revenueAccountId = await db.Accounts.AsNoTracking()
            .Where(a => a.Code == SystemAccountCodes.PatientRevenue).Select(a => (long?)a.Id).FirstOrDefaultAsync(ct);
        var receivableAccountId = await db.Accounts.AsNoTracking()
            .Where(a => a.Code == SystemAccountCodes.AccountsReceivable).Select(a => (long?)a.Id).FirstOrDefaultAsync(ct);
        if (revenueAccountId is null || receivableAccountId is null)
        {
            return; // System accounts not seeded for this environment yet.
        }

        var billBranchId = await db.Bills.AsNoTracking().Where(b => b.Id == billId).Select(b => b.BranchId).FirstOrDefaultAsync(ct);
        await PostCoreAsync(billBranchId, VoucherType.Journal, date, $"Credit note {creditNoteNumber} against bill {billNumber}",
            [(revenueAccountId.Value, amount, 0m, (string?)null), (receivableAccountId.Value, 0m, amount, null)],
            $"CREDIT_NOTE:{billId}", ct);
    }

    public async Task PostClaimSettlementAsync(long claimId, string claimNumber, long paymentModeId, decimal amount, DateOnly date, CancellationToken ct = default)
    {
        var mode = await db.PaymentModes.AsNoTracking().FirstOrDefaultAsync(m => m.Id == paymentModeId, ct);
        if (mode?.LinkedAccountId is not { } bankAccountId)
        {
            return; // This payment mode has no Finance account configured yet — nothing to post.
        }
        var revenueAccountId = await db.Accounts.AsNoTracking()
            .Where(a => a.Code == SystemAccountCodes.PatientRevenue).Select(a => (long?)a.Id).FirstOrDefaultAsync(ct);
        if (revenueAccountId is null)
        {
            return; // System accounts not seeded for this environment yet.
        }

        var claimBranchId = await db.Claims.AsNoTracking().Where(c => c.Id == claimId).Select(c => c.BranchId).FirstOrDefaultAsync(ct);
        await PostCoreAsync(claimBranchId, VoucherType.Receipt, date, $"Insurance claim {claimNumber} settled",
            [(bankAccountId, amount, 0m, (string?)null), (revenueAccountId.Value, 0m, amount, null)],
            $"CLAIM_SETTLEMENT:{claimId}", ct);
    }

    // ── Called by PayableService/ReceivableService (same feature, direct call) ──
    /// <summary>Debit Accounts Payable (reduces the liability) / Credit the chosen bank-cash account.</summary>
    internal async Task<long> PostPayableSettlementAsync(long branchId, long payableId, long cashAccountId, decimal amount, DateOnly date, string narration, CancellationToken ct)
    {
        var payableAccountId = await RequireSystemAccountAsync(SystemAccountCodes.AccountsPayable, ct);
        return await PostCoreAsync(branchId, VoucherType.Payment, date, narration,
            [(payableAccountId, amount, 0m, (string?)null), (cashAccountId, 0m, amount, null)], $"PAYABLE_SETTLEMENT:{payableId}", ct);
    }

    /// <summary>Debit the chosen bank-cash account / Credit Accounts Receivable (reduces the asset).</summary>
    internal async Task<long> PostReceivableCollectionAsync(long branchId, long receivableId, long cashAccountId, decimal amount, DateOnly date, string narration, CancellationToken ct)
    {
        var receivableAccountId = await RequireSystemAccountAsync(SystemAccountCodes.AccountsReceivable, ct);
        return await PostCoreAsync(branchId, VoucherType.Receipt, date, narration,
            [(cashAccountId, amount, 0m, (string?)null), (receivableAccountId, 0m, amount, null)], $"RECEIVABLE_COLLECTION:{receivableId}", ct);
    }

    private async Task<long> RequireSystemAccountAsync(string code, CancellationToken ct)
    {
        var id = await db.Accounts.AsNoTracking().Where(a => a.Code == code).Select(a => (long?)a.Id).FirstOrDefaultAsync(ct);
        return id ?? throw new DomainException($"System account '{code}' is not configured. Ask an administrator to seed the Chart of Accounts.");
    }

    private async Task<long> PostCoreAsync(
        long branchId, VoucherType type, DateOnly date, string narration,
        IReadOnlyList<(long AccountId, decimal DebitAmount, decimal CreditAmount, string? Narration)> lines,
        string? sourceReference, CancellationToken ct, long? reversalOfVoucherId = null)
    {
        var year = date.Year;
        var sequenceKey = type switch
        {
            VoucherType.Journal => SequenceKeys.JournalVoucherNumber,
            VoucherType.Payment => SequenceKeys.PaymentVoucherNumber,
            VoucherType.Receipt => SequenceKeys.ReceiptVoucherNumber,
            _ => throw new DomainException("Unknown voucher type."),
        };
        var prefix = type switch { VoucherType.Journal => "JV", VoucherType.Payment => "PV", VoucherType.Receipt => "RV", _ => "V" };
        var number = await sequences.NextValueAsync(sequenceKey, branchId, year, ct);

        var voucher = new Voucher
        {
            BranchId = branchId,
            VoucherNumber = $"{prefix}-{year}-{number.ToString().PadLeft(6, '0')}",
            VoucherType = type,
            VoucherDate = date,
            Narration = narration,
            PostedByUserId = currentUser.UserId,
            PostedAt = timeProvider.GetUtcNow(),
            ReversalOfVoucherId = reversalOfVoucherId,
            SourceReference = sourceReference,
        };
        foreach (var line in lines)
        {
            voucher.Lines.Add(new VoucherLine { AccountId = line.AccountId, DebitAmount = line.DebitAmount, CreditAmount = line.CreditAmount, Narration = line.Narration });
        }
        db.Vouchers.Add(voucher);
        await db.SaveChangesAsync(ct);

        foreach (var line in voucher.Lines)
        {
            db.LedgerEntries.Add(new LedgerEntry
            {
                BranchId = branchId, VoucherId = voucher.Id, VoucherLineId = line.Id, AccountId = line.AccountId,
                EntryDate = date, DebitAmount = line.DebitAmount, CreditAmount = line.CreditAmount, Narration = line.Narration,
            });
        }
        await activity.LogAsync(ActivityEvents.FinanceVoucherPosted, $"Voucher {voucher.VoucherNumber} posted", ct: ct);
        await db.SaveChangesAsync(ct);
        return voucher.Id;
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");

    private static VoucherDto ToDto(Voucher v) => new(
        v.Id, v.VoucherNumber, v.VoucherType, v.VoucherDate, v.Narration, v.Status,
        v.PostedByUser!.FullName, v.PostedAt, v.ReversalOfVoucherId, v.CancelReason, v.SourceReference,
        v.Lines.Select(l => new VoucherLineDto(l.AccountId, l.Account!.Code, l.Account.Name, l.DebitAmount, l.CreditAmount, l.Narration)).ToList());
}
