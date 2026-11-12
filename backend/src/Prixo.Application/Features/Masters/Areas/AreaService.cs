using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Areas;

public record AreaDto(long Id, string Code, string Name, long CityId, string CityName, bool IsActive);
public record SaveAreaRequest(string Code, string Name, long CityId, bool IsActive = true);

public class SaveAreaRequestValidator : AbstractValidator<SaveAreaRequest>
{
    public SaveAreaRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Code must be uppercase letters, digits, '-' or '_'.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CityId).GreaterThan(0);
    }
}

/// <summary>
/// Area-within-City master — a two-level lookup, so it sits outside the flat
/// SimpleMasterService framework (which has no FK dimension).
/// </summary>
public class AreaService(IAppDbContext db)
{
    public async Task<IReadOnlyList<AreaDto>> GetAllAsync(long? cityId = null, CancellationToken ct = default)
    {
        var query = db.Areas.AsNoTracking().Include(a => a.City).AsQueryable();
        if (cityId is { } id) query = query.Where(a => a.CityId == id);

        return await query
            .OrderBy(a => a.Name)
            .Select(a => new AreaDto(a.Id, a.Code, a.Name, a.CityId, a.City!.Name, a.IsActive))
            .ToListAsync(ct);
    }

    public async Task<long> CreateAsync(SaveAreaRequest request, CancellationToken ct = default)
    {
        await EnsureCityExistsAsync(request.CityId, ct);
        await EnsureUniqueCodeAsync(request.CityId, request.Code, null, ct);

        var area = new Area { Code = request.Code, Name = request.Name, CityId = request.CityId, IsActive = request.IsActive };
        db.Areas.Add(area);
        await db.SaveChangesAsync(ct);
        return area.Id;
    }

    public async Task UpdateAsync(long id, SaveAreaRequest request, CancellationToken ct = default)
    {
        var area = await db.Areas.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("Area", id);
        await EnsureCityExistsAsync(request.CityId, ct);
        await EnsureUniqueCodeAsync(request.CityId, request.Code, id, ct);

        area.Code = request.Code;
        area.Name = request.Name;
        area.CityId = request.CityId;
        area.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var area = await db.Areas.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("Area", id);
        if (await db.Patients.AnyAsync(p => p.AreaId == id, ct))
        {
            throw new DomainException("This area is in use by one or more patients and cannot be deleted.");
        }
        db.Areas.Remove(area); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureCityExistsAsync(long cityId, CancellationToken ct)
    {
        if (!await db.Cities.AnyAsync(c => c.Id == cityId, ct))
        {
            throw new NotFoundException("City", cityId);
        }
    }

    private async Task EnsureUniqueCodeAsync(long cityId, string code, long? selfId, CancellationToken ct)
    {
        if (await db.Areas.AnyAsync(a => a.CityId == cityId && a.Code == code && a.Id != selfId, ct))
        {
            throw new DomainException($"Area code '{code}' already exists in this city.");
        }
    }
}
