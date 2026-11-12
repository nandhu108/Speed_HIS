using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Ot;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Ot;

/// <summary>Branch-scoped theatres and their equipment — same shape as Pharmacy's StoreService (Store/Rack).</summary>
public class OtMasterService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<TheatreDto>> GetTheatresAsync(CancellationToken ct = default) =>
        await db.Theatres.AsNoTracking().OrderBy(t => t.Name)
            .Select(t => new TheatreDto(t.Id, t.Code, t.Name, t.IsActive))
            .ToListAsync(ct);

    public async Task<long> CreateTheatreAsync(SaveTheatreRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        await EnsureUniqueTheatreCodeAsync(branchId, request.Code, null, ct);
        var theatre = new Theatre { BranchId = branchId, Code = request.Code, Name = request.Name, IsActive = request.IsActive };
        db.Theatres.Add(theatre);
        await db.SaveChangesAsync(ct);
        return theatre.Id;
    }

    public async Task UpdateTheatreAsync(long id, SaveTheatreRequest request, CancellationToken ct = default)
    {
        var theatre = await db.Theatres.FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException(nameof(Theatre), id);
        await EnsureUniqueTheatreCodeAsync(theatre.BranchId, request.Code, id, ct);
        theatre.Code = request.Code;
        theatre.Name = request.Name;
        theatre.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EquipmentDto>> GetEquipmentAsync(long? theatreId, CancellationToken ct = default)
    {
        var equipment = db.Equipment.AsNoTracking();
        if (theatreId is { } id) equipment = equipment.Where(e => e.TheatreId == id);
        return await equipment.Include(e => e.Theatre).OrderBy(e => e.Name)
            .Select(e => new EquipmentDto(e.Id, e.Code, e.Name, e.TheatreId, e.Theatre != null ? e.Theatre.Name : null, e.IsActive))
            .ToListAsync(ct);
    }

    public async Task<long> CreateEquipmentAsync(SaveEquipmentRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        if (request.TheatreId is { } theatreId && !await db.Theatres.AnyAsync(t => t.Id == theatreId, ct))
        {
            throw new NotFoundException(nameof(Theatre), theatreId);
        }
        await EnsureUniqueEquipmentCodeAsync(branchId, request.Code, null, ct);
        var equipment = new Equipment
        {
            BranchId = branchId, Code = request.Code, Name = request.Name, TheatreId = request.TheatreId, IsActive = request.IsActive,
        };
        db.Equipment.Add(equipment);
        await db.SaveChangesAsync(ct);
        return equipment.Id;
    }

    public async Task UpdateEquipmentAsync(long id, SaveEquipmentRequest request, CancellationToken ct = default)
    {
        var equipment = await db.Equipment.FirstOrDefaultAsync(e => e.Id == id, ct) ?? throw new NotFoundException(nameof(Equipment), id);
        if (request.TheatreId is { } theatreId && !await db.Theatres.AnyAsync(t => t.Id == theatreId, ct))
        {
            throw new NotFoundException(nameof(Theatre), theatreId);
        }
        await EnsureUniqueEquipmentCodeAsync(equipment.BranchId, request.Code, id, ct);
        equipment.Code = request.Code;
        equipment.Name = request.Name;
        equipment.TheatreId = request.TheatreId;
        equipment.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");

    private async Task EnsureUniqueTheatreCodeAsync(long branchId, string code, long? selfId, CancellationToken ct)
    {
        if (await db.Theatres.AnyAsync(t => t.BranchId == branchId && t.Code == code && t.Id != selfId, ct))
        {
            throw new DomainException($"Theatre code '{code}' already exists for this branch.");
        }
    }

    private async Task EnsureUniqueEquipmentCodeAsync(long branchId, string code, long? selfId, CancellationToken ct)
    {
        if (await db.Equipment.AnyAsync(e => e.BranchId == branchId && e.Code == code && e.Id != selfId, ct))
        {
            throw new DomainException($"Equipment code '{code}' already exists for this branch.");
        }
    }
}
