using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Billing;

public record DiscountTypeDto(long Id, string Code, string Name, decimal MaxPercent,
    decimal ApprovalThresholdPercent, bool IsActive);
public record SaveDiscountTypeRequest(string Code, string Name, decimal MaxPercent,
    decimal ApprovalThresholdPercent, bool IsActive = true);

public class SaveDiscountTypeRequestValidator : AbstractValidator<SaveDiscountTypeRequest>
{
    public SaveDiscountTypeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MaxPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.ApprovalThresholdPercent).InclusiveBetween(0, 100)
            .LessThanOrEqualTo(x => x.MaxPercent)
            .WithMessage("Approval threshold cannot exceed the maximum percent.");
    }
}

/// <summary>Discount kinds with limits; Phase 6 billing enforces the approval workflow.</summary>
public class DiscountTypeService(IAppDbContext db)
{
    public async Task<IReadOnlyList<DiscountTypeDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.DiscountTypes.AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DiscountTypeDto(d.Id, d.Code, d.Name, d.MaxPercent,
                d.ApprovalThresholdPercent, d.IsActive))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(SaveDiscountTypeRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(request.Code, null, ct);
        var discountType = new DiscountType
        {
            Code = request.Code,
            Name = request.Name,
            MaxPercent = request.MaxPercent,
            ApprovalThresholdPercent = request.ApprovalThresholdPercent,
            IsActive = request.IsActive,
        };
        db.DiscountTypes.Add(discountType);
        await db.SaveChangesAsync(ct);
        return discountType.Id;
    }

    public async Task UpdateAsync(long id, SaveDiscountTypeRequest request, CancellationToken ct = default)
    {
        var discountType = await db.DiscountTypes.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException(nameof(DiscountType), id);
        await EnsureUniqueCodeAsync(request.Code, id, ct);

        discountType.Code = request.Code;
        discountType.Name = request.Name;
        discountType.MaxPercent = request.MaxPercent;
        discountType.ApprovalThresholdPercent = request.ApprovalThresholdPercent;
        discountType.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var discountType = await db.DiscountTypes.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException(nameof(DiscountType), id);
        db.DiscountTypes.Remove(discountType); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueCodeAsync(string code, long? selfId, CancellationToken ct)
    {
        if (await db.DiscountTypes.AnyAsync(d => d.Code == code && d.Id != selfId, ct))
        {
            throw new DomainException($"Discount type code '{code}' already exists.");
        }
    }
}
