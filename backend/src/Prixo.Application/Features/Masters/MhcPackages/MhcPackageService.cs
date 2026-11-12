using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.MhcPackages;

public record MhcPackageDto(long Id, string Code, string Name, string? Description, decimal Price, bool IsActive);
public record SaveMhcPackageRequest(string Code, string Name, string? Description, decimal Price, bool IsActive = true);

public class SaveMhcPackageRequestValidator : AbstractValidator<SaveMhcPackageRequest>
{
    public SaveMhcPackageRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Code must be uppercase letters, digits, '-' or '_'.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

/// <summary>MHC checkup package master — bespoke (not the flat SimpleMasterService) since it carries a Price field.</summary>
public class MhcPackageService(IAppDbContext db)
{
    public async Task<IReadOnlyList<MhcPackageDto>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var query = db.MhcPackages.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Code, term) || EF.Functions.ILike(p.Name, term));
        }

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new MhcPackageDto(p.Id, p.Code, p.Name, p.Description, p.Price, p.IsActive))
            .ToListAsync(ct);
    }

    public async Task<long> CreateAsync(SaveMhcPackageRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(request.Code, null, ct);

        var package = new MhcPackage
        {
            Code = request.Code, Name = request.Name, Description = request.Description,
            Price = request.Price, IsActive = request.IsActive,
        };
        db.MhcPackages.Add(package);
        await db.SaveChangesAsync(ct);
        return package.Id;
    }

    public async Task UpdateAsync(long id, SaveMhcPackageRequest request, CancellationToken ct = default)
    {
        var package = await db.MhcPackages.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(MhcPackage), id);
        await EnsureUniqueCodeAsync(request.Code, id, ct);

        package.Code = request.Code;
        package.Name = request.Name;
        package.Description = request.Description;
        package.Price = request.Price;
        package.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var package = await db.MhcPackages.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(MhcPackage), id);
        db.MhcPackages.Remove(package); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueCodeAsync(string code, long? selfId, CancellationToken ct)
    {
        if (await db.MhcPackages.AnyAsync(p => p.Code == code && p.Id != selfId, ct))
        {
            throw new DomainException($"MHC package code '{code}' already exists.");
        }
    }
}
