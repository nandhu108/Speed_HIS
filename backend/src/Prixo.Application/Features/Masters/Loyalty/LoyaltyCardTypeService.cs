using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Loyalty;

public record LoyaltyCardTypeDto(
    long Id, string Code, string Name, string? Description, int MemberLimit,
    long? LoyaltyCardCategoryId, string? LoyaltyCardCategoryName, bool IsActive);

public record SaveLoyaltyCardTypeRequest(
    string Code, string Name, string? Description, int MemberLimit, long? LoyaltyCardCategoryId, bool IsActive = true);

public class SaveLoyaltyCardTypeRequestValidator : AbstractValidator<SaveLoyaltyCardTypeRequest>
{
    public SaveLoyaltyCardTypeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Code must be uppercase letters, digits, '-' or '_'.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.MemberLimit).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Loyalty card product master — bespoke (not the flat SimpleMasterService) since it carries MemberLimit + an optional category FK.</summary>
public class LoyaltyCardTypeService(IAppDbContext db)
{
    public async Task<IReadOnlyList<LoyaltyCardTypeDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.LoyaltyCardTypes.AsNoTracking()
            .Include(t => t.LoyaltyCardCategory)
            .OrderBy(t => t.Name)
            .Select(t => ToDto(t))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(SaveLoyaltyCardTypeRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesAsync(request, null, ct);

        var type = new LoyaltyCardType
        {
            Code = request.Code, Name = request.Name, Description = request.Description,
            MemberLimit = request.MemberLimit, LoyaltyCardCategoryId = request.LoyaltyCardCategoryId,
            IsActive = request.IsActive,
        };
        db.LoyaltyCardTypes.Add(type);
        await db.SaveChangesAsync(ct);
        return type.Id;
    }

    public async Task UpdateAsync(long id, SaveLoyaltyCardTypeRequest request, CancellationToken ct = default)
    {
        var type = await db.LoyaltyCardTypes.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(LoyaltyCardType), id);
        await EnsureReferencesAsync(request, id, ct);

        type.Code = request.Code;
        type.Name = request.Name;
        type.Description = request.Description;
        type.MemberLimit = request.MemberLimit;
        type.LoyaltyCardCategoryId = request.LoyaltyCardCategoryId;
        type.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var type = await db.LoyaltyCardTypes.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(LoyaltyCardType), id);
        if (await db.LoyaltyCards.AnyAsync(c => c.LoyaltyCardTypeId == id, ct))
        {
            throw new DomainException("This card type is in use by one or more loyalty cards and cannot be deleted.");
        }
        db.LoyaltyCardTypes.Remove(type);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureReferencesAsync(SaveLoyaltyCardTypeRequest request, long? selfId, CancellationToken ct)
    {
        if (request.LoyaltyCardCategoryId is { } categoryId && !await db.LoyaltyCardCategories.AnyAsync(c => c.Id == categoryId, ct))
        {
            throw new NotFoundException(nameof(LoyaltyCardCategory), categoryId);
        }
        if (await db.LoyaltyCardTypes.AnyAsync(t => t.Code == request.Code && t.Id != selfId, ct))
        {
            throw new DomainException($"Loyalty card type code '{request.Code}' already exists.");
        }
    }

    private static LoyaltyCardTypeDto ToDto(LoyaltyCardType t) => new(
        t.Id, t.Code, t.Name, t.Description, t.MemberLimit,
        t.LoyaltyCardCategoryId, t.LoyaltyCardCategory?.Name, t.IsActive);
}
