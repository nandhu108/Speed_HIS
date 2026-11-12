using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Finance;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Finance;

/// <summary>Chart of Accounts CRUD (plan §18). A small bespoke service rather than the generic simple-master framework, since Account needs AccountType/ParentAccountId, not a slot in ISimpleMaster.</summary>
public class AccountService(IAppDbContext db)
{
    public async Task<IReadOnlyList<AccountDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Accounts.AsNoTracking().Include(a => a.ParentAccount).OrderBy(a => a.Code)
            .Select(a => new AccountDto(a.Id, a.Code, a.Name, a.AccountType, a.ParentAccountId, a.ParentAccount != null ? a.ParentAccount.Name : null, a.IsSystemAccount, a.Description, a.IsActive))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(SaveAccountRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(request.Code, null, ct);
        await EnsureParentExistsAsync(request.ParentAccountId, ct);
        var account = new Account { Code = request.Code, Name = request.Name, AccountType = request.AccountType, ParentAccountId = request.ParentAccountId, Description = request.Description };
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);
        return account.Id;
    }

    public async Task UpdateAsync(long id, SaveAccountRequest request, CancellationToken ct = default)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct) ?? throw new NotFoundException(nameof(Account), id);
        if (account.IsSystemAccount)
        {
            throw new DomainException("System accounts cannot be renamed or re-coded — only their description may change elsewhere.");
        }
        await EnsureUniqueCodeAsync(request.Code, id, ct);
        await EnsureParentExistsAsync(request.ParentAccountId, ct);
        if (request.ParentAccountId == id)
        {
            throw new DomainException("An account cannot be its own parent.");
        }
        account.Code = request.Code;
        account.Name = request.Name;
        account.AccountType = request.AccountType;
        account.ParentAccountId = request.ParentAccountId;
        account.Description = request.Description;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct) ?? throw new NotFoundException(nameof(Account), id);
        if (account.IsSystemAccount)
        {
            throw new DomainException("System accounts cannot be deleted.");
        }
        if (await db.VoucherLines.AnyAsync(l => l.AccountId == id, ct))
        {
            throw new DomainException("This account has posted voucher lines and cannot be deleted.");
        }
        if (await db.Accounts.AnyAsync(a => a.ParentAccountId == id, ct))
        {
            throw new DomainException("This account has child accounts and cannot be deleted.");
        }
        account.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueCodeAsync(string code, long? excludeId, CancellationToken ct)
    {
        if (await db.Accounts.AnyAsync(a => a.Code == code && a.Id != (excludeId ?? 0), ct))
        {
            throw new DomainException($"Account code '{code}' is already in use.");
        }
    }

    private async Task EnsureParentExistsAsync(long? parentAccountId, CancellationToken ct)
    {
        if (parentAccountId is { } id && !await db.Accounts.AnyAsync(a => a.Id == id, ct))
        {
            throw new NotFoundException(nameof(Account), id);
        }
    }
}
