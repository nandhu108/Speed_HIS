using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Drugs;

public record DrugDto(long Id, string Code, string Name, long? GenericId, string? GenericName,
    long? ManufacturerId, string? ManufacturerName, string? Form, string? Strength,
    long UomId, string UomName, string? HsnCode, long? TaxId, string? TaxName,
    string? Schedule, bool IsHighAlert, bool IsActive);
public record SaveDrugRequest(string Code, string Name, long? GenericId, long? ManufacturerId,
    string? Form, string? Strength, long UomId, string? HsnCode, long? TaxId,
    string? Schedule, bool IsHighAlert = false, bool IsActive = true);

public class SaveDrugRequestValidator : AbstractValidator<SaveDrugRequest>
{
    private static readonly string[] Schedules = ["H", "H1", "X"];

    public SaveDrugRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Form).MaximumLength(50);
        RuleFor(x => x.Strength).MaximumLength(50);
        RuleFor(x => x.UomId).GreaterThan(0);
        RuleFor(x => x.HsnCode).MaximumLength(20);
        RuleFor(x => x.Schedule)
            .Must(s => s is null || Schedules.Contains(s))
            .WithMessage("Schedule must be H, H1 or X.");
    }
}

/// <summary>Global drug catalog. Deep pharmacy behavior (batches, racks, FEFO) lands in Phase 8.</summary>
public class DrugService(IAppDbContext db)
{
    public async Task<PagedResult<DrugDto>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        var drugs = db.Drugs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            drugs = drugs.Where(d =>
                EF.Functions.ILike(d.Code, term) ||
                EF.Functions.ILike(d.Name, term) ||
                (d.Generic != null && EF.Functions.ILike(d.Generic.Name, term)));
        }

        var totalCount = await drugs.CountAsync(ct);
        var items = await drugs
            .OrderBy(d => d.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(d => new DrugDto(d.Id, d.Code, d.Name,
                d.GenericId, d.Generic != null ? d.Generic.Name : null,
                d.ManufacturerId, d.Manufacturer != null ? d.Manufacturer.Name : null,
                d.Form, d.Strength, d.UomId, d.Uom!.Name, d.HsnCode,
                d.TaxId, d.Tax != null ? d.Tax.Name : null, d.Schedule, d.IsHighAlert, d.IsActive))
            .ToListAsync(ct);

        return PagedResult<DrugDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<long> CreateAsync(SaveDrugRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesAsync(request, null, ct);
        var drug = new Drug
        {
            Code = request.Code,
            Name = request.Name,
            GenericId = request.GenericId,
            ManufacturerId = request.ManufacturerId,
            Form = request.Form,
            Strength = request.Strength,
            UomId = request.UomId,
            HsnCode = request.HsnCode,
            TaxId = request.TaxId,
            Schedule = request.Schedule,
            IsHighAlert = request.IsHighAlert,
            IsActive = request.IsActive,
        };
        db.Drugs.Add(drug);
        await db.SaveChangesAsync(ct);
        return drug.Id;
    }

    public async Task UpdateAsync(long id, SaveDrugRequest request, CancellationToken ct = default)
    {
        var drug = await db.Drugs.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException(nameof(Drug), id);
        await EnsureReferencesAsync(request, id, ct);

        drug.Code = request.Code;
        drug.Name = request.Name;
        drug.GenericId = request.GenericId;
        drug.ManufacturerId = request.ManufacturerId;
        drug.Form = request.Form;
        drug.Strength = request.Strength;
        drug.UomId = request.UomId;
        drug.HsnCode = request.HsnCode;
        drug.TaxId = request.TaxId;
        drug.Schedule = request.Schedule;
        drug.IsHighAlert = request.IsHighAlert;
        drug.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var drug = await db.Drugs.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException(nameof(Drug), id);
        db.Drugs.Remove(drug); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureReferencesAsync(SaveDrugRequest request, long? selfId, CancellationToken ct)
    {
        if (await db.Drugs.AnyAsync(d => d.Code == request.Code && d.Id != selfId, ct))
        {
            throw new DomainException($"Drug code '{request.Code}' already exists.");
        }
        if (request.GenericId is not null && !await db.Generics.AnyAsync(g => g.Id == request.GenericId, ct))
        {
            throw new DomainException("Generic does not exist.");
        }
        if (request.ManufacturerId is not null
            && !await db.Manufacturers.AnyAsync(m => m.Id == request.ManufacturerId, ct))
        {
            throw new DomainException("Manufacturer does not exist.");
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
