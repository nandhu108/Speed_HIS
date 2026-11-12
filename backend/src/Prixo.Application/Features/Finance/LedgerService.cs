using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Finance;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Finance;

/// <summary>Per-account ledger viewer (plan §18) — reads the LedgerEntry projection, not VoucherLines, so it never needs to join through Voucher.</summary>
public class LedgerService(IAppDbContext db)
{
    public async Task<AccountLedgerDto> GetAccountLedgerAsync(long accountId, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default)
    {
        var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId, ct)
            ?? throw new NotFoundException(nameof(Account), accountId);

        var opening = fromDate is { } from
            ? await SignedSumAsync(account.AccountType, db.LedgerEntries.AsNoTracking().Where(e => e.AccountId == accountId && e.EntryDate < from), ct)
            : 0m;

        var entries = db.LedgerEntries.AsNoTracking().Include(e => e.Voucher).Where(e => e.AccountId == accountId);
        if (fromDate is { } f) entries = entries.Where(e => e.EntryDate >= f);
        if (toDate is { } t) entries = entries.Where(e => e.EntryDate <= t);

        var rows = await entries.OrderBy(e => e.EntryDate).ThenBy(e => e.Id)
            .Select(e => new { e.Id, e.EntryDate, VoucherNumber = e.Voucher!.VoucherNumber, Narration = e.Narration ?? e.Voucher.Narration, e.DebitAmount, e.CreditAmount })
            .ToListAsync(ct);

        var running = opening;
        var isDebitNormal = account.AccountType is AccountType.Asset or AccountType.Expense;
        var result = new List<LedgerEntryRowDto>(rows.Count);
        foreach (var row in rows)
        {
            running += isDebitNormal ? row.DebitAmount - row.CreditAmount : row.CreditAmount - row.DebitAmount;
            result.Add(new LedgerEntryRowDto(row.Id, row.EntryDate, row.VoucherNumber, row.Narration, row.DebitAmount, row.CreditAmount, running));
        }

        return new AccountLedgerDto(account.Id, account.Code, account.Name, opening, result, running);
    }

    private static async Task<decimal> SignedSumAsync(AccountType accountType, IQueryable<LedgerEntry> query, CancellationToken ct)
    {
        var totals = await query.GroupBy(_ => 1).Select(g => new { Debit = g.Sum(e => e.DebitAmount), Credit = g.Sum(e => e.CreditAmount) }).FirstOrDefaultAsync(ct);
        if (totals is null) return 0m;
        return accountType is AccountType.Asset or AccountType.Expense ? totals.Debit - totals.Credit : totals.Credit - totals.Debit;
    }
}
