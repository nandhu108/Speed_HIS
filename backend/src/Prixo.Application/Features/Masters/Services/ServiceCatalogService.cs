using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Services;

public record ServiceDto(long Id, string Code, string Name, long ServiceCategoryId,
    string CategoryName, long? DepartmentId, string? DepartmentName, bool IsTaxable,
    long? TaxId, string? TaxName, decimal DefaultRate, bool IsActive);
public record SaveServiceRequest(string Code, string Name, long ServiceCategoryId,
    long? DepartmentId, bool IsTaxable, long? TaxId, decimal DefaultRate, bool IsActive = true);

public class SaveServiceRequestValidator : AbstractValidator<SaveServiceRequest>
{
    public SaveServiceRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ServiceCategoryId).GreaterThan(0);
        RuleFor(x => x.DefaultRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxId).NotNull().When(x => x.IsTaxable)
            .WithMessage("Select the tax slab for a taxable service.");
    }
}

/// <summary>Global chargeable-service catalog. Branch pricing lives in tariffs.</summary>
public class ServiceCatalogService(IAppDbContext db)
{
    public async Task<PagedResult<ServiceDto>> GetPagedAsync(
        PagedQuery query, long? categoryId = null, CancellationToken ct = default)
    {
        var services = db.Services.AsNoTracking();

        if (categoryId is not null)
        {
            services = services.Where(s => s.ServiceCategoryId == categoryId);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            services = services.Where(s =>
                EF.Functions.ILike(s.Code, term) || EF.Functions.ILike(s.Name, term));
        }

        var totalCount = await services.CountAsync(ct);
        var items = await services
            .OrderBy(s => s.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new ServiceDto(s.Id, s.Code, s.Name, s.ServiceCategoryId,
                s.Category!.Name, s.DepartmentId,
                s.Department != null ? s.Department.Name : null,
                s.IsTaxable, s.TaxId, s.Tax != null ? s.Tax.Name : null,
                s.DefaultRate, s.IsActive))
            .ToListAsync(ct);

        return PagedResult<ServiceDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<long> CreateAsync(SaveServiceRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesAsync(request, null, ct);

        var service = new Service
        {
            Code = request.Code,
            Name = request.Name,
            ServiceCategoryId = request.ServiceCategoryId,
            DepartmentId = request.DepartmentId,
            IsTaxable = request.IsTaxable,
            TaxId = request.IsTaxable ? request.TaxId : null,
            DefaultRate = request.DefaultRate,
            IsActive = request.IsActive,
        };
        db.Services.Add(service);
        await db.SaveChangesAsync(ct);
        return service.Id;
    }

    public async Task UpdateAsync(long id, SaveServiceRequest request, CancellationToken ct = default)
    {
        var service = await db.Services.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException(nameof(Service), id);
        await EnsureReferencesAsync(request, id, ct);

        service.Code = request.Code;
        service.Name = request.Name;
        service.ServiceCategoryId = request.ServiceCategoryId;
        service.DepartmentId = request.DepartmentId;
        service.IsTaxable = request.IsTaxable;
        service.TaxId = request.IsTaxable ? request.TaxId : null;
        service.DefaultRate = request.DefaultRate;
        service.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var service = await db.Services.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException(nameof(Service), id);
        if (await db.TariffItems.AnyAsync(i => i.ServiceId == id, ct))
        {
            throw new DomainException("Service is priced in tariffs. Remove those rows first.");
        }
        if (await db.PackageItems.AnyAsync(i => i.ServiceId == id, ct))
        {
            throw new DomainException("Service is bundled in packages. Remove those rows first.");
        }

        db.Services.Remove(service); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureReferencesAsync(SaveServiceRequest request, long? selfId, CancellationToken ct)
    {
        if (await db.Services.AnyAsync(s => s.Code == request.Code && s.Id != selfId, ct))
        {
            throw new DomainException($"Service code '{request.Code}' already exists.");
        }
        if (!await db.ServiceCategories.AnyAsync(c => c.Id == request.ServiceCategoryId, ct))
        {
            throw new DomainException("Service category does not exist.");
        }
        if (request.DepartmentId is not null && !await db.Departments.AnyAsync(d => d.Id == request.DepartmentId, ct))
        {
            throw new DomainException("Department does not exist.");
        }
        if (request.IsTaxable && !await db.Taxes.AnyAsync(t => t.Id == request.TaxId, ct))
        {
            throw new DomainException("Tax slab does not exist.");
        }
    }
}
