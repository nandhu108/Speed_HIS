using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Admin.Designations;

public record DesignationDto(long Id, string Name, string? Description, bool IsActive, int UserCount);
public record SaveDesignationRequest(string Name, string? Description, bool IsActive = true);

public class SaveDesignationRequestValidator : AbstractValidator<SaveDesignationRequest>
{
    public SaveDesignationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class DesignationService(IAppDbContext db)
{
    public async Task<IReadOnlyList<DesignationDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Designations.AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DesignationDto(d.Id, d.Name, d.Description, d.IsActive,
                db.Users.Count(u => u.DesignationId == d.Id)))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(SaveDesignationRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueNameAsync(request.Name, null, ct);
        var designation = new Designation
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
        };
        db.Designations.Add(designation);
        await db.SaveChangesAsync(ct);
        return designation.Id;
    }

    public async Task UpdateAsync(long id, SaveDesignationRequest request, CancellationToken ct = default)
    {
        var designation = await db.Designations.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException(nameof(Designation), id);
        await EnsureUniqueNameAsync(request.Name, id, ct);

        designation.Name = request.Name;
        designation.Description = request.Description;
        designation.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var designation = await db.Designations.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException(nameof(Designation), id);
        if (await db.Users.AnyAsync(u => u.DesignationId == id, ct))
        {
            throw new DomainException("Designation is assigned to users. Reassign them first.");
        }

        db.Designations.Remove(designation); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueNameAsync(string name, long? selfId, CancellationToken ct)
    {
        if (await db.Designations.AnyAsync(d => d.Name == name && d.Id != selfId, ct))
        {
            throw new DomainException($"Designation '{name}' already exists.");
        }
    }
}
