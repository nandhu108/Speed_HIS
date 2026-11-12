using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Common;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Common;

public record SimpleMasterDto(long Id, string Code, string Name, string? Description, bool IsActive);
public record SaveSimpleMasterRequest(string Code, string Name, string? Description, bool IsActive = true);

public class SaveSimpleMasterRequestValidator : AbstractValidator<SaveSimpleMasterRequest>
{
    public SaveSimpleMasterRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Code must be uppercase letters, digits, '-' or '_'.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

/// <summary>
/// The generic master CRUD framework (plan Phase 2): one service reused by every
/// simple code+name reference master. Subclasses add delete guards where other
/// tables reference the master.
/// </summary>
public abstract class SimpleMasterService<TEntity>(IAppDbContext db)
    where TEntity : BaseEntity, ISimpleMaster
{
    protected IAppDbContext Db => db;
    protected abstract string DisplayName { get; }

    /// <summary>Instantiates the entity; needed because required members block a new() constraint.</summary>
    protected abstract TEntity NewEntity(string code, string name);

    public async Task<IReadOnlyList<SimpleMasterDto>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var query = db.Set<TEntity>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(e => EF.Functions.ILike(e.Code, term) || EF.Functions.ILike(e.Name, term));
        }

        return await query
            .OrderBy(e => e.Name)
            .Select(e => new SimpleMasterDto(e.Id, e.Code, e.Name, e.Description, e.IsActive))
            .ToListAsync(ct);
    }

    public async Task<long> CreateAsync(SaveSimpleMasterRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(request.Code, null, ct);
        var entity = NewEntity(request.Code, request.Name);
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        db.Set<TEntity>().Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(long id, SaveSimpleMasterRequest request, CancellationToken ct = default)
    {
        var entity = await db.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException(DisplayName, id);
        await EnsureUniqueCodeAsync(request.Code, id, ct);

        entity.Code = request.Code;
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException(DisplayName, id);
        await EnsureDeletableAsync(id, ct);

        db.Set<TEntity>().Remove(entity); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Override to block deletion while other records reference this master.</summary>
    protected virtual Task EnsureDeletableAsync(long id, CancellationToken ct) => Task.CompletedTask;

    protected void ThrowInUse(string usedBy) =>
        throw new DomainException($"{DisplayName} is used by {usedBy}. Remove those references first.");

    private async Task EnsureUniqueCodeAsync(string code, long? selfId, CancellationToken ct)
    {
        if (await db.Set<TEntity>().AnyAsync(e => e.Code == code && e.Id != selfId, ct))
        {
            throw new DomainException($"{DisplayName} code '{code}' already exists.");
        }
    }
}
