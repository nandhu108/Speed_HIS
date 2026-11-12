using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Billing;

public record TaxDto(long Id, string Code, string Name, decimal RatePercent, bool IsActive);
public record SaveTaxRequest(string Code, string Name, decimal RatePercent, bool IsActive = true);

public class SaveTaxRequestValidator : AbstractValidator<SaveTaxRequest>
{
    public SaveTaxRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RatePercent).InclusiveBetween(0, 100);
    }
}

/// <summary>GST slabs (global).</summary>
public class TaxService(IAppDbContext db)
{
    public async Task<IReadOnlyList<TaxDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Taxes.AsNoTracking()
            .OrderBy(t => t.RatePercent)
            .Select(t => new TaxDto(t.Id, t.Code, t.Name, t.RatePercent, t.IsActive))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(SaveTaxRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(request.Code, null, ct);
        var tax = new Tax
        {
            Code = request.Code,
            Name = request.Name,
            RatePercent = request.RatePercent,
            IsActive = request.IsActive,
        };
        db.Taxes.Add(tax);
        await db.SaveChangesAsync(ct);
        return tax.Id;
    }

    public async Task UpdateAsync(long id, SaveTaxRequest request, CancellationToken ct = default)
    {
        var tax = await db.Taxes.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(Tax), id);
        await EnsureUniqueCodeAsync(request.Code, id, ct);

        tax.Code = request.Code;
        tax.Name = request.Name;
        tax.RatePercent = request.RatePercent;
        tax.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var tax = await db.Taxes.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(Tax), id);
        if (await db.Services.AnyAsync(s => s.TaxId == id, ct)
            || await db.Items.AnyAsync(i => i.TaxId == id, ct)
            || await db.Drugs.AnyAsync(d => d.TaxId == id, ct))
        {
            throw new DomainException("Tax slab is used by services, items or drugs. Reassign them first.");
        }

        db.Taxes.Remove(tax); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueCodeAsync(string code, long? selfId, CancellationToken ct)
    {
        if (await db.Taxes.AnyAsync(t => t.Code == code && t.Id != selfId, ct))
        {
            throw new DomainException($"Tax code '{code}' already exists.");
        }
    }
}
