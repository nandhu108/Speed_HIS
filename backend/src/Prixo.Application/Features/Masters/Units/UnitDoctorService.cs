using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Units;

/// <summary>"Link Unit &amp; Doctors" — a bare join, mirrors DoctorDepartment's shape but
/// managed as its own full-replace sub-resource (reached from the Unit side) rather than
/// diffed inline on a parent-entity save request.</summary>
public class UnitDoctorService(IAppDbContext db)
{
    public async Task<IReadOnlyList<UnitDoctorDto>> GetByUnitIdAsync(long unitId, CancellationToken ct = default)
    {
        if (!await db.Units.AnyAsync(u => u.Id == unitId, ct))
        {
            throw new NotFoundException(nameof(Unit), unitId);
        }

        return await db.UnitDoctors.AsNoTracking()
            .Where(ud => ud.UnitId == unitId)
            .Include(ud => ud.Doctor).ThenInclude(d => d!.User)
            .OrderBy(ud => ud.Doctor!.User!.FullName)
            .Select(ud => new UnitDoctorDto(ud.DoctorId, ud.Doctor!.User!.FullName))
            .ToListAsync(ct);
    }

    public async Task ReplaceDoctorsAsync(long unitId, ReplaceUnitDoctorsRequest request, CancellationToken ct = default)
    {
        if (!await db.Units.AnyAsync(u => u.Id == unitId, ct))
        {
            throw new NotFoundException(nameof(Unit), unitId);
        }

        var doctorIds = request.DoctorIds.Distinct().ToList();
        var validCount = await db.Doctors.CountAsync(d => doctorIds.Contains(d.Id), ct);
        if (validCount != doctorIds.Count)
        {
            throw new DomainException("One or more doctors do not exist.");
        }

        var existing = await db.UnitDoctors.Where(ud => ud.UnitId == unitId).ToListAsync(ct);
        db.UnitDoctors.RemoveRange(existing);

        foreach (var doctorId in doctorIds)
        {
            db.UnitDoctors.Add(new UnitDoctor { UnitId = unitId, DoctorId = doctorId });
        }

        await db.SaveChangesAsync(ct);
    }
}
