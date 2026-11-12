using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Mrd;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Mrd;

/// <summary>Record Retention policies (plan §17: "policy per record type") — a small bespoke CRUD, not the generic simple-master framework, because RetentionYears has no slot there.</summary>
public class RetentionPolicyService(IAppDbContext db)
{
    public async Task<IReadOnlyList<RetentionPolicyDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.RetentionPolicies.AsNoTracking().OrderBy(p => p.Name)
            .Select(p => new RetentionPolicyDto(p.Id, p.Code, p.Name, p.RetentionYears, p.Description, p.IsActive))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(SaveRetentionPolicyRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(request.Code, null, ct);
        var policy = new RetentionPolicy
        {
            Code = request.Code, Name = request.Name, RetentionYears = request.RetentionYears, Description = request.Description,
        };
        db.RetentionPolicies.Add(policy);
        await db.SaveChangesAsync(ct);
        return policy.Id;
    }

    public async Task UpdateAsync(long id, SaveRetentionPolicyRequest request, CancellationToken ct = default)
    {
        var policy = await db.RetentionPolicies.FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw new NotFoundException(nameof(RetentionPolicy), id);
        await EnsureUniqueCodeAsync(request.Code, id, ct);
        policy.Code = request.Code;
        policy.Name = request.Name;
        policy.RetentionYears = request.RetentionYears;
        policy.Description = request.Description;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var policy = await db.RetentionPolicies.FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw new NotFoundException(nameof(RetentionPolicy), id);
        if (await db.DestructionRegisterEntries.AnyAsync(d => d.RetentionPolicyId == id, ct))
        {
            throw new DomainException("This retention policy is referenced by the destruction register and cannot be deleted.");
        }
        policy.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueCodeAsync(string code, long? excludeId, CancellationToken ct)
    {
        if (await db.RetentionPolicies.AnyAsync(p => p.Code == code && p.Id != (excludeId ?? 0), ct))
        {
            throw new DomainException($"Retention policy code '{code}' is already in use.");
        }
    }
}
