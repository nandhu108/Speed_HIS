using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Loyalty;

public record LoyaltyCardPriceListItemDto(
    long Id, long LoyaltyCardTypeId, string LoyaltyCardTypeName, DateOnly EffectiveFrom, DateOnly? EffectiveTo, int ItemCount, bool IsActive);

public record LoyaltyCardPriceItemDto(long Id, int DurationMonths, decimal Amount);

public record LoyaltyCardPriceDetailDto(
    long Id, long LoyaltyCardTypeId, string LoyaltyCardTypeName, DateOnly EffectiveFrom, DateOnly? EffectiveTo,
    bool IsActive, IReadOnlyList<LoyaltyCardPriceItemDto> Items);

public record SaveLoyaltyCardPriceItemRequest(int DurationMonths, decimal Amount);

public record SaveLoyaltyCardPriceRequest(
    long LoyaltyCardTypeId, DateOnly EffectiveFrom, DateOnly? EffectiveTo,
    IReadOnlyList<SaveLoyaltyCardPriceItemRequest> Items, bool IsActive = true);

public class SaveLoyaltyCardPriceRequestValidator : AbstractValidator<SaveLoyaltyCardPriceRequest>
{
    public SaveLoyaltyCardPriceRequestValidator()
    {
        RuleFor(x => x.LoyaltyCardTypeId).GreaterThan(0);
        RuleFor(x => x.EffectiveTo).GreaterThanOrEqualTo(x => x.EffectiveFrom).When(x => x.EffectiveTo is not null)
            .WithMessage("Effective-to must be on or after effective-from.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("A price list needs at least one duration/amount line.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.DurationMonths).GreaterThan(0);
            item.RuleFor(i => i.Amount).GreaterThanOrEqualTo(0);
        });
    }
}

/// <summary>
/// Branch price list per card type with an effective window — the "Loyalty Card Price
/// Matrix" (Type x window x duration -> price). Header+child CRUD, mirrors BillEstimateTemplateService.
/// </summary>
public class LoyaltyCardPriceService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<LoyaltyCardPriceListItemDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.LoyaltyCardPrices.AsNoTracking()
            .Include(p => p.LoyaltyCardType)
            .OrderByDescending(p => p.EffectiveFrom)
            .Select(p => new LoyaltyCardPriceListItemDto(
                p.Id, p.LoyaltyCardTypeId, p.LoyaltyCardType!.Name, p.EffectiveFrom, p.EffectiveTo, p.Items.Count, p.IsActive))
            .ToListAsync(ct);

    public async Task<LoyaltyCardPriceDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var price = await db.LoyaltyCardPrices.AsNoTracking()
            .Include(p => p.LoyaltyCardType)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(LoyaltyCardPrice), id);

        return new LoyaltyCardPriceDetailDto(
            price.Id, price.LoyaltyCardTypeId, price.LoyaltyCardType!.Name, price.EffectiveFrom, price.EffectiveTo, price.IsActive,
            price.Items.OrderBy(i => i.DurationMonths).Select(i => new LoyaltyCardPriceItemDto(i.Id, i.DurationMonths, i.Amount)).ToList());
    }

    public async Task<long> CreateAsync(SaveLoyaltyCardPriceRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesAsync(request, null, ct);

        var price = new LoyaltyCardPrice
        {
            BranchId = RequireBranch(),
            LoyaltyCardTypeId = request.LoyaltyCardTypeId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsActive = request.IsActive,
            Items = request.Items.Select(ToItem).ToList(),
        };
        db.LoyaltyCardPrices.Add(price);
        await db.SaveChangesAsync(ct);
        return price.Id;
    }

    public async Task UpdateAsync(long id, SaveLoyaltyCardPriceRequest request, CancellationToken ct = default)
    {
        var price = await db.LoyaltyCardPrices.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(LoyaltyCardPrice), id);
        await EnsureReferencesAsync(request, id, ct);

        price.LoyaltyCardTypeId = request.LoyaltyCardTypeId;
        price.EffectiveFrom = request.EffectiveFrom;
        price.EffectiveTo = request.EffectiveTo;
        price.IsActive = request.IsActive;

        foreach (var row in price.Items.ToList())
        {
            db.LoyaltyCardPriceItems.Remove(row);
            price.Items.Remove(row);
        }
        foreach (var item in request.Items)
        {
            price.Items.Add(ToItem(item));
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var price = await db.LoyaltyCardPrices.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(LoyaltyCardPrice), id);

        foreach (var item in price.Items)
        {
            db.LoyaltyCardPriceItems.Remove(item);
        }
        db.LoyaltyCardPrices.Remove(price);
        await db.SaveChangesAsync(ct);
    }

    private static LoyaltyCardPriceItem ToItem(SaveLoyaltyCardPriceItemRequest i) => new()
    {
        DurationMonths = i.DurationMonths,
        Amount = i.Amount,
    };

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("A branch context is required.");

    private async Task EnsureReferencesAsync(SaveLoyaltyCardPriceRequest request, long? selfId, CancellationToken ct)
    {
        if (!await db.LoyaltyCardTypes.AnyAsync(t => t.Id == request.LoyaltyCardTypeId, ct))
        {
            throw new NotFoundException(nameof(LoyaltyCardType), request.LoyaltyCardTypeId);
        }
        var branchId = RequireBranch();
        if (await db.LoyaltyCardPrices.AnyAsync(p =>
                p.BranchId == branchId && p.LoyaltyCardTypeId == request.LoyaltyCardTypeId &&
                p.EffectiveFrom == request.EffectiveFrom && p.Id != selfId, ct))
        {
            throw new DomainException("A price list already exists for this card type with the same effective-from date.");
        }
    }
}
