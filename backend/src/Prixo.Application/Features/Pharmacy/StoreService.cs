using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Pharmacy;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Pharmacy;

/// <summary>Branch-scoped stock-holding locations (main pharmacy store, OT store, ward store…) and their racks.</summary>
public class StoreService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<StoreDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Stores.AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new StoreDto(s.Id, s.Code, s.Name, s.StoreType, s.IsDefault, s.IsActive))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(SaveStoreRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        await EnsureUniqueCodeAsync(branchId, request.Code, null, ct);

        if (request.IsDefault)
        {
            await ClearExistingDefaultAsync(branchId, ct);
        }

        var store = new Domain.Entities.Pharmacy.Store
        {
            BranchId = branchId,
            Code = request.Code,
            Name = request.Name,
            StoreType = request.StoreType,
            IsDefault = request.IsDefault,
            IsActive = request.IsActive,
        };
        db.Stores.Add(store);
        await db.SaveChangesAsync(ct);
        return store.Id;
    }

    public async Task UpdateAsync(long id, SaveStoreRequest request, CancellationToken ct = default)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Pharmacy.Store), id);
        await EnsureUniqueCodeAsync(store.BranchId, request.Code, id, ct);

        if (request.IsDefault && !store.IsDefault)
        {
            await ClearExistingDefaultAsync(store.BranchId, ct);
        }

        store.Code = request.Code;
        store.Name = request.Name;
        store.StoreType = request.StoreType;
        store.IsDefault = request.IsDefault;
        store.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RackDto>> GetRacksAsync(long? storeId, CancellationToken ct = default)
    {
        var racks = db.Racks.AsNoTracking();
        if (storeId is { } id) racks = racks.Where(r => r.StoreId == id);
        return await racks.Include(r => r.Store).OrderBy(r => r.Name)
            .Select(r => new RackDto(r.Id, r.StoreId, r.Store!.Name, r.Code, r.Name))
            .ToListAsync(ct);
    }

    public async Task<long> CreateRackAsync(SaveRackRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        if (!await db.Stores.AnyAsync(s => s.Id == request.StoreId, ct))
        {
            throw new NotFoundException(nameof(Domain.Entities.Pharmacy.Store), request.StoreId);
        }
        var rack = new Rack { BranchId = branchId, StoreId = request.StoreId, Code = request.Code, Name = request.Name };
        db.Racks.Add(rack);
        await db.SaveChangesAsync(ct);
        return rack.Id;
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");

    private async Task ClearExistingDefaultAsync(long branchId, CancellationToken ct)
    {
        var current = await db.Stores.Where(s => s.BranchId == branchId && s.IsDefault).ToListAsync(ct);
        foreach (var store in current)
        {
            store.IsDefault = false;
        }
    }

    private async Task EnsureUniqueCodeAsync(long branchId, string code, long? selfId, CancellationToken ct)
    {
        if (await db.Stores.AnyAsync(s => s.BranchId == branchId && s.Code == code && s.Id != selfId, ct))
        {
            throw new DomainException($"Store code '{code}' already exists for this branch.");
        }
    }
}
