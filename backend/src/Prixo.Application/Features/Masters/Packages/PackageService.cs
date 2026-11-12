using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Packages;

public record PackageListItemDto(long Id, string Code, string Name, decimal Price,
    int ValidityDays, int ItemCount, bool IsActive);
public record PackageItemDto(long Id, long ServiceId, string ServiceCode, string ServiceName,
    decimal Quantity, decimal DefaultRate);
public record PackageDetailDto(long Id, string Code, string Name, string? Description,
    decimal Price, int ValidityDays, bool IsActive, IReadOnlyList<PackageItemDto> Items);
public record SavePackageItemRequest(long ServiceId, decimal Quantity);
public record SavePackageRequest(string Code, string Name, string? Description, decimal Price,
    int ValidityDays, IReadOnlyList<SavePackageItemRequest> Items, bool IsActive = true);

public class SavePackageRequestValidator : AbstractValidator<SavePackageRequest>
{
    public SavePackageRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ValidityDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Items).NotEmpty().WithMessage("A package needs at least one service.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ServiceId).GreaterThan(0);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.ServiceId).Distinct().Count() == items.Count)
            .WithMessage("A service appears more than once in the package.");
    }
}

/// <summary>Fixed-price service bundles. Branch-scoped.</summary>
public class PackageService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<PackageListItemDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Packages.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new PackageListItemDto(p.Id, p.Code, p.Name, p.Price,
                p.ValidityDays, p.Items.Count, p.IsActive))
            .ToListAsync(ct);

    public async Task<PackageDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var package = await db.Packages.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Service)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(Package), id);

        return new PackageDetailDto(package.Id, package.Code, package.Name, package.Description,
            package.Price, package.ValidityDays, package.IsActive,
            package.Items.OrderBy(i => i.Service!.Name)
                .Select(i => new PackageItemDto(i.Id, i.ServiceId, i.Service!.Code,
                    i.Service.Name, i.Quantity, i.Service.DefaultRate))
                .ToList());
    }

    public async Task<long> CreateAsync(SavePackageRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesAsync(request, null, ct);

        var package = new Package
        {
            BranchId = RequireBranch(),
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            ValidityDays = request.ValidityDays,
            IsActive = request.IsActive,
            Items = request.Items
                .Select(i => new PackageItem { ServiceId = i.ServiceId, Quantity = i.Quantity }).ToList(),
        };
        db.Packages.Add(package);
        await db.SaveChangesAsync(ct);
        return package.Id;
    }

    public async Task UpdateAsync(long id, SavePackageRequest request, CancellationToken ct = default)
    {
        var package = await db.Packages
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(Package), id);
        await EnsureReferencesAsync(request, id, ct);

        package.Code = request.Code;
        package.Name = request.Name;
        package.Description = request.Description;
        package.Price = request.Price;
        package.ValidityDays = request.ValidityDays;
        package.IsActive = request.IsActive;

        var desired = request.Items.ToDictionary(i => i.ServiceId, i => i.Quantity);
        foreach (var row in package.Items.Where(i => !desired.ContainsKey(i.ServiceId)).ToList())
        {
            db.PackageItems.Remove(row);
            package.Items.Remove(row);
        }
        foreach (var row in package.Items)
        {
            row.Quantity = desired[row.ServiceId];
        }
        foreach (var (serviceId, quantity) in desired.Where(d => package.Items.All(i => i.ServiceId != d.Key)))
        {
            package.Items.Add(new PackageItem { PackageId = id, ServiceId = serviceId, Quantity = quantity });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var package = await db.Packages.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(Package), id);

        foreach (var item in package.Items)
        {
            db.PackageItems.Remove(item);
        }
        db.Packages.Remove(package); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private long RequireBranch() =>
        currentUser.BranchId ?? throw new DomainException("A branch context is required.");

    private async Task EnsureReferencesAsync(SavePackageRequest request, long? selfId, CancellationToken ct)
    {
        if (await db.Packages.AnyAsync(p => p.Code == request.Code && p.Id != selfId, ct))
        {
            throw new DomainException($"Package code '{request.Code}' already exists.");
        }
        var serviceIds = request.Items.Select(i => i.ServiceId).Distinct().ToList();
        var validServices = await db.Services.CountAsync(s => serviceIds.Contains(s.Id), ct);
        if (validServices != serviceIds.Count)
        {
            throw new DomainException("One or more services do not exist.");
        }
    }
}
