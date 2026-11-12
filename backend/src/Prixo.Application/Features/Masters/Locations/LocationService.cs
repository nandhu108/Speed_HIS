using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Locations;

// Building → floor → ward → room → bed hierarchy, managed on a single screen.
// Everything is branch-scoped; the global filter confines reads to the caller's branch.

public record BuildingDto(long Id, string Code, string Name, bool IsActive, int FloorCount);
public record FloorDto(long Id, string Code, string Name, long BuildingId, string BuildingName, bool IsActive, int WardCount);
public record RoomTypeDto(long Id, string Code, string Name, decimal BedChargePerDay, bool IsActive, int RoomCount);
public record RoomTypeChargeDto(long Id, decimal Rate, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
public record SaveRoomTypeChargeRequest(decimal Rate, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
public record WardDto(long Id, string Code, string Name, long FloorId, string FloorName, bool IsIcu, bool IsObservation, bool IsActive, int RoomCount);
public record RoomDto(long Id, string Code, string Name, long WardId, string WardName,
    long RoomTypeId, string RoomTypeName, bool IsActive, int BedCount);
public record BedDto(long Id, string Code, long RoomId, string RoomName, string WardName, bool IsActive);

public record SaveBuildingRequest(string Code, string Name, bool IsActive = true);
public record SaveFloorRequest(string Code, string Name, long BuildingId, bool IsActive = true);
public record SaveRoomTypeRequest(string Code, string Name, decimal BedChargePerDay, bool IsActive = true);
public record SaveWardRequest(string Code, string Name, long FloorId, bool IsIcu, bool IsObservation = false, bool IsActive = true);
public record SaveRoomRequest(string Code, string Name, long WardId, long RoomTypeId, bool IsActive = true);
public record SaveBedRequest(string Code, long RoomId, bool IsActive = true);

public class SaveBuildingRequestValidator : AbstractValidator<SaveBuildingRequest>
{
    public SaveBuildingRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class SaveFloorRequestValidator : AbstractValidator<SaveFloorRequest>
{
    public SaveFloorRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BuildingId).GreaterThan(0);
    }
}

public class SaveRoomTypeRequestValidator : AbstractValidator<SaveRoomTypeRequest>
{
    public SaveRoomTypeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BedChargePerDay).GreaterThanOrEqualTo(0);
    }
}

public class SaveRoomTypeChargeRequestValidator : AbstractValidator<SaveRoomTypeChargeRequest>
{
    public SaveRoomTypeChargeRequestValidator()
    {
        RuleFor(x => x.Rate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EffectiveTo).GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo is not null)
            .WithMessage("Effective-to must be on or after effective-from.");
    }
}

public class SaveWardRequestValidator : AbstractValidator<SaveWardRequest>
{
    public SaveWardRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FloorId).GreaterThan(0);
    }
}

public class SaveRoomRequestValidator : AbstractValidator<SaveRoomRequest>
{
    public SaveRoomRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.WardId).GreaterThan(0);
        RuleFor(x => x.RoomTypeId).GreaterThan(0);
    }
}

public class SaveBedRequestValidator : AbstractValidator<SaveBedRequest>
{
    public SaveBedRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.RoomId).GreaterThan(0);
    }
}

public class LocationService(IAppDbContext db, ICurrentUser currentUser)
{
    // ── Buildings ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<BuildingDto>> GetBuildingsAsync(CancellationToken ct = default) =>
        await db.Buildings.AsNoTracking().OrderBy(b => b.Name)
            .Select(b => new BuildingDto(b.Id, b.Code, b.Name, b.IsActive,
                db.Floors.Count(f => f.BuildingId == b.Id)))
            .ToListAsync(ct);

    public async Task<long> CreateBuildingAsync(SaveBuildingRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueAsync(db.Buildings, request.Code, null, "Building", ct);
        var building = new Building
        {
            BranchId = RequireBranch(),
            Code = request.Code,
            Name = request.Name,
            IsActive = request.IsActive,
        };
        db.Buildings.Add(building);
        await db.SaveChangesAsync(ct);
        return building.Id;
    }

    public async Task UpdateBuildingAsync(long id, SaveBuildingRequest request, CancellationToken ct = default)
    {
        var building = await db.Buildings.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new NotFoundException(nameof(Building), id);
        await EnsureUniqueAsync(db.Buildings, request.Code, id, "Building", ct);
        building.Code = request.Code;
        building.Name = request.Name;
        building.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteBuildingAsync(long id, CancellationToken ct = default)
    {
        var building = await db.Buildings.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new NotFoundException(nameof(Building), id);
        if (await db.Floors.AnyAsync(f => f.BuildingId == id, ct))
        {
            throw new DomainException("Building has floors. Delete them first.");
        }
        db.Buildings.Remove(building);
        await db.SaveChangesAsync(ct);
    }

    // ── Floors ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<FloorDto>> GetFloorsAsync(CancellationToken ct = default) =>
        await db.Floors.AsNoTracking().OrderBy(f => f.Building!.Name).ThenBy(f => f.Name)
            .Select(f => new FloorDto(f.Id, f.Code, f.Name, f.BuildingId, f.Building!.Name, f.IsActive,
                db.Wards.Count(w => w.FloorId == f.Id)))
            .ToListAsync(ct);

    public async Task<long> CreateFloorAsync(SaveFloorRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueAsync(db.Floors, request.Code, null, "Floor", ct);
        await EnsureExistsAsync(db.Buildings, request.BuildingId, "Building", ct);
        var floor = new Floor
        {
            BranchId = RequireBranch(),
            Code = request.Code,
            Name = request.Name,
            BuildingId = request.BuildingId,
            IsActive = request.IsActive,
        };
        db.Floors.Add(floor);
        await db.SaveChangesAsync(ct);
        return floor.Id;
    }

    public async Task UpdateFloorAsync(long id, SaveFloorRequest request, CancellationToken ct = default)
    {
        var floor = await db.Floors.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NotFoundException(nameof(Floor), id);
        await EnsureUniqueAsync(db.Floors, request.Code, id, "Floor", ct);
        await EnsureExistsAsync(db.Buildings, request.BuildingId, "Building", ct);
        floor.Code = request.Code;
        floor.Name = request.Name;
        floor.BuildingId = request.BuildingId;
        floor.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteFloorAsync(long id, CancellationToken ct = default)
    {
        var floor = await db.Floors.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NotFoundException(nameof(Floor), id);
        if (await db.Wards.AnyAsync(w => w.FloorId == id, ct))
        {
            throw new DomainException("Floor has wards. Delete them first.");
        }
        db.Floors.Remove(floor);
        await db.SaveChangesAsync(ct);
    }

    // ── Room types ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<RoomTypeDto>> GetRoomTypesAsync(CancellationToken ct = default) =>
        await db.RoomTypes.AsNoTracking().OrderBy(r => r.Name)
            .Select(r => new RoomTypeDto(r.Id, r.Code, r.Name, r.BedChargePerDay, r.IsActive,
                db.Rooms.Count(room => room.RoomTypeId == r.Id)))
            .ToListAsync(ct);

    public async Task<long> CreateRoomTypeAsync(SaveRoomTypeRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueAsync(db.RoomTypes, request.Code, null, "Room type", ct);
        var roomType = new RoomType
        {
            BranchId = RequireBranch(),
            Code = request.Code,
            Name = request.Name,
            BedChargePerDay = request.BedChargePerDay,
            IsActive = request.IsActive,
        };
        db.RoomTypes.Add(roomType);
        await db.SaveChangesAsync(ct);
        return roomType.Id;
    }

    public async Task UpdateRoomTypeAsync(long id, SaveRoomTypeRequest request, CancellationToken ct = default)
    {
        var roomType = await db.RoomTypes.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(RoomType), id);
        await EnsureUniqueAsync(db.RoomTypes, request.Code, id, "Room type", ct);
        roomType.Code = request.Code;
        roomType.Name = request.Name;
        roomType.BedChargePerDay = request.BedChargePerDay;
        roomType.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteRoomTypeAsync(long id, CancellationToken ct = default)
    {
        var roomType = await db.RoomTypes.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(RoomType), id);
        if (await db.Rooms.AnyAsync(room => room.RoomTypeId == id, ct))
        {
            throw new DomainException("Room type is used by rooms. Reassign them first.");
        }
        db.RoomTypes.Remove(roomType);
        await db.SaveChangesAsync(ct);
    }

    // ── Room type charges (date-effective bed-charge revisions) ──────────────
    // Same "rate revision = a new row" philosophy as Tariff — no update action, only
    // add/delete, so a later rate change never rewrites history (AdmissionService snapshots
    // the resolved rate onto BedAllocation at allocation time).

    public async Task<IReadOnlyList<RoomTypeChargeDto>> GetRoomTypeChargesAsync(long roomTypeId, CancellationToken ct = default)
    {
        await EnsureExistsAsync(db.RoomTypes, roomTypeId, "Room type", ct);
        return await db.RoomTypeCharges.AsNoTracking().Where(c => c.RoomTypeId == roomTypeId)
            .OrderByDescending(c => c.EffectiveFrom)
            .Select(c => new RoomTypeChargeDto(c.Id, c.Rate, c.EffectiveFrom, c.EffectiveTo))
            .ToListAsync(ct);
    }

    public async Task<long> AddRoomTypeChargeAsync(long roomTypeId, SaveRoomTypeChargeRequest request, CancellationToken ct = default)
    {
        await EnsureExistsAsync(db.RoomTypes, roomTypeId, "Room type", ct);
        var charge = new RoomTypeCharge
        {
            BranchId = RequireBranch(),
            RoomTypeId = roomTypeId,
            Rate = request.Rate,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
        };
        db.RoomTypeCharges.Add(charge);
        await db.SaveChangesAsync(ct);
        return charge.Id;
    }

    public async Task DeleteRoomTypeChargeAsync(long roomTypeId, long id, CancellationToken ct = default)
    {
        var charge = await db.RoomTypeCharges.FirstOrDefaultAsync(c => c.Id == id && c.RoomTypeId == roomTypeId, ct)
            ?? throw new NotFoundException(nameof(RoomTypeCharge), id);
        db.RoomTypeCharges.Remove(charge);
        await db.SaveChangesAsync(ct);
    }

    // ── Wards ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<WardDto>> GetWardsAsync(CancellationToken ct = default) =>
        await db.Wards.AsNoTracking().OrderBy(w => w.Name)
            .Select(w => new WardDto(w.Id, w.Code, w.Name, w.FloorId, w.Floor!.Name, w.IsIcu, w.IsObservation, w.IsActive,
                db.Rooms.Count(r => r.WardId == w.Id)))
            .ToListAsync(ct);

    public async Task<long> CreateWardAsync(SaveWardRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueAsync(db.Wards, request.Code, null, "Ward", ct);
        await EnsureExistsAsync(db.Floors, request.FloorId, "Floor", ct);
        var ward = new Ward
        {
            BranchId = RequireBranch(),
            Code = request.Code,
            Name = request.Name,
            FloorId = request.FloorId,
            IsIcu = request.IsIcu,
            IsObservation = request.IsObservation,
            IsActive = request.IsActive,
        };
        db.Wards.Add(ward);
        await db.SaveChangesAsync(ct);
        return ward.Id;
    }

    public async Task UpdateWardAsync(long id, SaveWardRequest request, CancellationToken ct = default)
    {
        var ward = await db.Wards.FirstOrDefaultAsync(w => w.Id == id, ct)
            ?? throw new NotFoundException(nameof(Ward), id);
        await EnsureUniqueAsync(db.Wards, request.Code, id, "Ward", ct);
        await EnsureExistsAsync(db.Floors, request.FloorId, "Floor", ct);
        ward.Code = request.Code;
        ward.Name = request.Name;
        ward.FloorId = request.FloorId;
        ward.IsIcu = request.IsIcu;
        ward.IsObservation = request.IsObservation;
        ward.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteWardAsync(long id, CancellationToken ct = default)
    {
        var ward = await db.Wards.FirstOrDefaultAsync(w => w.Id == id, ct)
            ?? throw new NotFoundException(nameof(Ward), id);
        if (await db.Rooms.AnyAsync(r => r.WardId == id, ct))
        {
            throw new DomainException("Ward has rooms. Delete them first.");
        }
        db.Wards.Remove(ward);
        await db.SaveChangesAsync(ct);
    }

    // ── Rooms ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<RoomDto>> GetRoomsAsync(CancellationToken ct = default) =>
        await db.Rooms.AsNoTracking().OrderBy(r => r.Ward!.Name).ThenBy(r => r.Name)
            .Select(r => new RoomDto(r.Id, r.Code, r.Name, r.WardId, r.Ward!.Name,
                r.RoomTypeId, r.RoomType!.Name, r.IsActive,
                db.Beds.Count(b => b.RoomId == r.Id)))
            .ToListAsync(ct);

    public async Task<long> CreateRoomAsync(SaveRoomRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueAsync(db.Rooms, request.Code, null, "Room", ct);
        await EnsureExistsAsync(db.Wards, request.WardId, "Ward", ct);
        await EnsureExistsAsync(db.RoomTypes, request.RoomTypeId, "Room type", ct);
        var room = new Room
        {
            BranchId = RequireBranch(),
            Code = request.Code,
            Name = request.Name,
            WardId = request.WardId,
            RoomTypeId = request.RoomTypeId,
            IsActive = request.IsActive,
        };
        db.Rooms.Add(room);
        await db.SaveChangesAsync(ct);
        return room.Id;
    }

    public async Task UpdateRoomAsync(long id, SaveRoomRequest request, CancellationToken ct = default)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Room), id);
        await EnsureUniqueAsync(db.Rooms, request.Code, id, "Room", ct);
        await EnsureExistsAsync(db.Wards, request.WardId, "Ward", ct);
        await EnsureExistsAsync(db.RoomTypes, request.RoomTypeId, "Room type", ct);
        room.Code = request.Code;
        room.Name = request.Name;
        room.WardId = request.WardId;
        room.RoomTypeId = request.RoomTypeId;
        room.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteRoomAsync(long id, CancellationToken ct = default)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Room), id);
        if (await db.Beds.AnyAsync(b => b.RoomId == id, ct))
        {
            throw new DomainException("Room has beds. Delete them first.");
        }
        db.Rooms.Remove(room);
        await db.SaveChangesAsync(ct);
    }

    // ── Beds ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<BedDto>> GetBedsAsync(long? roomId = null, CancellationToken ct = default)
    {
        var beds = db.Beds.AsNoTracking();
        if (roomId is not null)
        {
            beds = beds.Where(b => b.RoomId == roomId);
        }
        return await beds.OrderBy(b => b.Code)
            .Select(b => new BedDto(b.Id, b.Code, b.RoomId, b.Room!.Name, b.Room.Ward!.Name, b.IsActive))
            .ToListAsync(ct);
    }

    public async Task<long> CreateBedAsync(SaveBedRequest request, CancellationToken ct = default)
    {
        if (await db.Beds.AnyAsync(b => b.Code == request.Code, ct))
        {
            throw new DomainException($"Bed code '{request.Code}' already exists.");
        }
        await EnsureExistsAsync(db.Rooms, request.RoomId, "Room", ct);
        var bed = new Bed
        {
            BranchId = RequireBranch(),
            Code = request.Code,
            RoomId = request.RoomId,
            IsActive = request.IsActive,
        };
        db.Beds.Add(bed);
        await db.SaveChangesAsync(ct);
        return bed.Id;
    }

    public async Task UpdateBedAsync(long id, SaveBedRequest request, CancellationToken ct = default)
    {
        var bed = await db.Beds.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new NotFoundException(nameof(Bed), id);
        if (await db.Beds.AnyAsync(b => b.Code == request.Code && b.Id != id, ct))
        {
            throw new DomainException($"Bed code '{request.Code}' already exists.");
        }
        await EnsureExistsAsync(db.Rooms, request.RoomId, "Room", ct);
        bed.Code = request.Code;
        bed.RoomId = request.RoomId;
        bed.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteBedAsync(long id, CancellationToken ct = default)
    {
        var bed = await db.Beds.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new NotFoundException(nameof(Bed), id);
        db.Beds.Remove(bed);
        await db.SaveChangesAsync(ct);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private long RequireBranch() =>
        currentUser.BranchId ?? throw new DomainException("A branch context is required.");

    private static async Task EnsureUniqueAsync<T>(
        DbSet<T> set, string code, long? selfId, string displayName, CancellationToken ct)
        where T : Domain.Common.BranchEntity
    {
        if (await set.AnyAsync(e => EF.Property<string>(e, "Code") == code && e.Id != selfId, ct))
        {
            throw new DomainException($"{displayName} code '{code}' already exists.");
        }
    }

    private static async Task EnsureExistsAsync<T>(
        DbSet<T> set, long id, string displayName, CancellationToken ct)
        where T : Domain.Common.BaseEntity
    {
        if (!await set.AnyAsync(e => e.Id == id, ct))
        {
            throw new DomainException($"{displayName} does not exist.");
        }
    }
}
