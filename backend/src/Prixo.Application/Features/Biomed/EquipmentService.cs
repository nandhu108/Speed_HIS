using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Biomed;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Biomed;

/// <summary>Equipment Master + Location tracking (plan §19). CurrentLocation is a snapshot kept in sync by an append-only EquipmentLocationLog, same idiom as MRD's RecordFile.</summary>
public class EquipmentService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<long> CreateAsync(SaveEquipmentRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        if (await db.BiomedEquipment.AnyAsync(e => e.EquipmentCode == request.EquipmentCode, ct))
        {
            throw new DomainException($"Equipment code '{request.EquipmentCode}' is already in use.");
        }
        var equipment = new BiomedEquipment
        {
            BranchId = branchId, EquipmentCode = request.EquipmentCode, Name = request.Name, SerialNumber = request.SerialNumber,
            Model = request.Model, Manufacturer = request.Manufacturer, DepartmentId = request.DepartmentId, VendorId = request.VendorId,
            Criticality = request.Criticality, CurrentLocation = request.CurrentLocation, PurchaseDate = request.PurchaseDate,
            PurchaseCost = request.PurchaseCost, WarrantyExpiryDate = request.WarrantyExpiryDate, Notes = request.Notes,
        };
        db.BiomedEquipment.Add(equipment);
        await db.SaveChangesAsync(ct);
        return equipment.Id;
    }

    public async Task UpdateAsync(long id, SaveEquipmentRequest request, CancellationToken ct = default)
    {
        var equipment = await db.BiomedEquipment.FirstOrDefaultAsync(e => e.Id == id, ct) ?? throw new NotFoundException(nameof(BiomedEquipment), id);
        if (await db.BiomedEquipment.AnyAsync(e => e.EquipmentCode == request.EquipmentCode && e.Id != id, ct))
        {
            throw new DomainException($"Equipment code '{request.EquipmentCode}' is already in use.");
        }
        equipment.EquipmentCode = request.EquipmentCode;
        equipment.Name = request.Name;
        equipment.SerialNumber = request.SerialNumber;
        equipment.Model = request.Model;
        equipment.Manufacturer = request.Manufacturer;
        equipment.DepartmentId = request.DepartmentId;
        equipment.VendorId = request.VendorId;
        equipment.Criticality = request.Criticality;
        equipment.PurchaseDate = request.PurchaseDate;
        equipment.PurchaseCost = request.PurchaseCost;
        equipment.WarrantyExpiryDate = request.WarrantyExpiryDate;
        equipment.Notes = request.Notes;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EquipmentListItemDto>> GetListAsync(EquipmentQuery query, CancellationToken ct = default)
    {
        var equipment = db.BiomedEquipment.AsNoTracking().Include(e => e.Department).AsQueryable();
        if (query.Status is { } status) equipment = equipment.Where(e => e.Status == status);
        if (query.Criticality is { } criticality) equipment = equipment.Where(e => e.Criticality == criticality);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            equipment = equipment.Where(e => EF.Functions.ILike(e.EquipmentCode, term) || EF.Functions.ILike(e.Name, term) || (e.SerialNumber != null && EF.Functions.ILike(e.SerialNumber, term)));
        }

        return await equipment.OrderBy(e => e.Name)
            .Select(e => new EquipmentListItemDto(e.Id, e.EquipmentCode, e.Name, e.SerialNumber, e.Model,
                e.Department != null ? e.Department.Name : null, e.Criticality, e.Status, e.CurrentLocation, e.WarrantyExpiryDate))
            .ToListAsync(ct);
    }

    public async Task<EquipmentDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var e = await db.BiomedEquipment.AsNoTracking().Include(x => x.Department).Include(x => x.Vendor)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(BiomedEquipment), id);
        return new EquipmentDetailDto(e.Id, e.EquipmentCode, e.Name, e.SerialNumber, e.Model, e.Manufacturer,
            e.DepartmentId, e.Department?.Name, e.VendorId, e.Vendor?.Name, e.Criticality, e.Status, e.CurrentLocation,
            e.PurchaseDate, e.PurchaseCost, e.WarrantyExpiryDate, e.Notes);
    }

    public async Task<long> RecordMovementAsync(long id, RecordEquipmentMovementRequest request, CancellationToken ct = default)
    {
        var equipment = await db.BiomedEquipment.FirstOrDefaultAsync(e => e.Id == id, ct) ?? throw new NotFoundException(nameof(BiomedEquipment), id);
        var log = new EquipmentLocationLog
        {
            EquipmentId = id, MovedAt = timeProvider.GetUtcNow(), FromLocation = equipment.CurrentLocation,
            ToLocation = request.ToLocation, Reason = request.Reason, MovedByUserId = currentUser.UserId,
        };
        db.EquipmentLocationLogs.Add(log);
        equipment.CurrentLocation = request.ToLocation;
        await db.SaveChangesAsync(ct);
        return log.Id;
    }

    public async Task<IReadOnlyList<EquipmentLocationLogDto>> GetLocationHistoryAsync(long id, CancellationToken ct = default) =>
        await db.EquipmentLocationLogs.AsNoTracking().Where(l => l.EquipmentId == id).Include(l => l.MovedByUser)
            .OrderByDescending(l => l.MovedAt)
            .Select(l => new EquipmentLocationLogDto(l.Id, l.MovedAt, l.FromLocation, l.ToLocation, l.Reason, l.MovedByUser!.FullName))
            .ToListAsync(ct);

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
