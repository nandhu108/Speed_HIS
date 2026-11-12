using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Items;

public record ItemDto(long Id, string Code, string Name, long ItemCategoryId, string CategoryName,
    long UomId, string UomName, string? HsnCode, long? TaxId, string? TaxName,
    decimal ReorderLevel, bool IsActive);
public record SaveItemRequest(string Code, string Name, long ItemCategoryId, long UomId,
    string? HsnCode, long? TaxId, decimal ReorderLevel, bool IsActive = true);

public class SaveItemRequestValidator : AbstractValidator<SaveItemRequest>
{
    public SaveItemRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ItemCategoryId).GreaterThan(0);
        RuleFor(x => x.UomId).GreaterThan(0);
        RuleFor(x => x.HsnCode).MaximumLength(20);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
    }
}

/// <summary>General-store item catalog (global). Stock arrives with the Phase 8/13 engine.</summary>
public class ItemService(IAppDbContext db)
{
    public async Task<PagedResult<ItemDto>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        var items = db.Items.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            items = items.Where(i =>
                EF.Functions.ILike(i.Code, term) || EF.Functions.ILike(i.Name, term));
        }

        var totalCount = await items.CountAsync(ct);
        var page = await items
            .OrderBy(i => i.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(i => new ItemDto(i.Id, i.Code, i.Name, i.ItemCategoryId, i.Category!.Name,
                i.UomId, i.Uom!.Name, i.HsnCode, i.TaxId, i.Tax != null ? i.Tax.Name : null,
                i.ReorderLevel, i.IsActive))
            .ToListAsync(ct);

        return PagedResult<ItemDto>.Create(page, query.Page, query.PageSize, totalCount);
    }

    public async Task<long> CreateAsync(SaveItemRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesAsync(request, null, ct);
        var item = new Item
        {
            Code = request.Code,
            Name = request.Name,
            ItemCategoryId = request.ItemCategoryId,
            UomId = request.UomId,
            HsnCode = request.HsnCode,
            TaxId = request.TaxId,
            ReorderLevel = request.ReorderLevel,
            IsActive = request.IsActive,
        };
        db.Items.Add(item);
        await db.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task UpdateAsync(long id, SaveItemRequest request, CancellationToken ct = default)
    {
        var item = await db.Items.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException(nameof(Item), id);
        await EnsureReferencesAsync(request, id, ct);

        item.Code = request.Code;
        item.Name = request.Name;
        item.ItemCategoryId = request.ItemCategoryId;
        item.UomId = request.UomId;
        item.HsnCode = request.HsnCode;
        item.TaxId = request.TaxId;
        item.ReorderLevel = request.ReorderLevel;
        item.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var item = await db.Items.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException(nameof(Item), id);
        db.Items.Remove(item); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureReferencesAsync(SaveItemRequest request, long? selfId, CancellationToken ct)
    {
        if (await db.Items.AnyAsync(i => i.Code == request.Code && i.Id != selfId, ct))
        {
            throw new DomainException($"Item code '{request.Code}' already exists.");
        }
        if (!await db.ItemCategories.AnyAsync(c => c.Id == request.ItemCategoryId, ct))
        {
            throw new DomainException("Item category does not exist.");
        }
        if (!await db.Uoms.AnyAsync(u => u.Id == request.UomId, ct))
        {
            throw new DomainException("Unit of measure does not exist.");
        }
        if (request.TaxId is not null && !await db.Taxes.AnyAsync(t => t.Id == request.TaxId, ct))
        {
            throw new DomainException("Tax slab does not exist.");
        }
    }
}
