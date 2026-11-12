using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Ipd;

/// <summary>
/// Bed status board and occupancy dashboard (plan §9). Occupied is derived-only, set
/// exclusively by AdmissionService's admit/transfer/discharge/cancel flows; this service
/// only toggles the other four statuses and reads the board.
/// </summary>
public class BedBoardService(IAppDbContext db)
{
    public async Task<IReadOnlyList<BedBoardItemDto>> GetBoardAsync(long? wardId, CancellationToken ct = default)
    {
        var beds = db.Beds.AsNoTracking().Include(b => b.Room).ThenInclude(r => r!.Ward)
            .Include(b => b.Room).ThenInclude(r => r!.RoomType).AsQueryable();
        if (wardId is { } ward) beds = beds.Where(b => b.Room!.WardId == ward);

        var bedList = await beds.OrderBy(b => b.Room!.Ward!.Name).ThenBy(b => b.Room!.Name).ThenBy(b => b.Code).ToListAsync(ct);
        var activeAdmissions = await db.Admissions.AsNoTracking()
            .Where(a => a.Status == AdmissionStatus.Admitted)
            .Include(a => a.Patient).Include(a => a.PrimaryConsultant).ThenInclude(d => d!.User)
            .Include(a => a.BedAllocations)
            .ToListAsync(ct);

        return bedList.Select(b =>
        {
            var admission = activeAdmissions.FirstOrDefault(a => a.CurrentBedId == b.Id);
            var allocation = admission?.BedAllocations.FirstOrDefault(ba => ba.ReleasedAt == null);
            return new BedBoardItemDto(
                b.Id, b.Code, b.Status, b.Room!.Ward!.Name, b.Room.Name, b.Room.RoomType!.Name,
                admission?.Id, admission?.AdmissionNumber,
                admission is null ? null : admission.Patient!.FirstName + " " + admission.Patient.LastName,
                admission?.PrimaryConsultant?.User?.FullName, allocation?.AllocatedAt);
        }).ToList();
    }

    public async Task SetStatusAsync(long bedId, SetBedStatusRequest request, CancellationToken ct = default)
    {
        var bed = await db.Beds.FirstOrDefaultAsync(b => b.Id == bedId, ct) ?? throw new NotFoundException(nameof(Bed), bedId);
        if (request.Status == BedStatus.Occupied)
        {
            throw new DomainException("Occupied is set automatically by admitting/transferring a patient, not toggled directly.");
        }
        if (bed.Status == BedStatus.Occupied)
        {
            throw new DomainException($"Bed {bed.Code} is occupied — transfer or discharge the patient first.");
        }
        bed.Status = request.Status;
        await db.SaveChangesAsync(ct);
    }

    public async Task<BedOccupancyDashboardDto> GetOccupancyDashboardAsync(CancellationToken ct = default)
    {
        var beds = await db.Beds.AsNoTracking().Include(b => b.Room).ThenInclude(r => r!.Ward).ToListAsync(ct);
        var totalBeds = beds.Count;
        var occupied = beds.Count(b => b.Status == BedStatus.Occupied);
        var byWard = beds.GroupBy(b => new { b.Room!.WardId, b.Room.Ward!.Name })
            .Select(g => new WardOccupancyDto(g.Key.WardId, g.Key.Name, g.Count(), g.Count(b => b.Status == BedStatus.Occupied),
                g.Count() == 0 ? 0 : Math.Round(g.Count(b => b.Status == BedStatus.Occupied) * 100m / g.Count(), 1)))
            .OrderBy(w => w.WardName)
            .ToList();

        return new BedOccupancyDashboardDto(
            totalBeds, occupied, beds.Count(b => b.Status == BedStatus.Vacant), beds.Count(b => b.Status == BedStatus.Reserved),
            beds.Count(b => b.Status == BedStatus.Cleaning), beds.Count(b => b.Status == BedStatus.Blocked),
            totalBeds == 0 ? 0 : Math.Round(occupied * 100m / totalBeds, 1), byWard);
    }
}
