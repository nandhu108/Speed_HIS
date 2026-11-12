using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Appointments;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Appointments;

/// <summary>Weekly recurring operating-hours for a Unit ("Schedule Unit - Day Wise") — purely
/// descriptive, mirrors DoctorScheduleService's block-replace shape minus the override
/// machinery (no date-specific leave exceptions for units in this pass).</summary>
public class UnitScheduleService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<UnitScheduleDto> GetByUnitIdAsync(long unitId, CancellationToken ct = default)
    {
        var unit = await db.Units.AsNoTracking().FirstOrDefaultAsync(u => u.Id == unitId, ct)
            ?? throw new NotFoundException(nameof(Unit), unitId);

        var blocks = await db.UnitSchedules.AsNoTracking()
            .Where(s => s.UnitId == unitId)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .Select(s => new UnitScheduleBlockDto(s.Id, s.DayOfWeek, s.StartTime, s.EndTime))
            .ToListAsync(ct);

        return new UnitScheduleDto(unit.Id, unit.Name, blocks);
    }

    public async Task ReplaceBlocksAsync(long unitId, ReplaceUnitScheduleRequest request, CancellationToken ct = default)
    {
        if (!await db.Units.AnyAsync(u => u.Id == unitId, ct))
        {
            throw new NotFoundException(nameof(Unit), unitId);
        }

        var existing = await db.UnitSchedules.Where(s => s.UnitId == unitId).ToListAsync(ct);
        db.UnitSchedules.RemoveRange(existing);

        foreach (var block in request.Blocks)
        {
            db.UnitSchedules.Add(new UnitSchedule
            {
                UnitId = unitId,
                BranchId = currentUser.BranchId ?? throw new DomainException("Select a branch first."),
                DayOfWeek = block.DayOfWeek,
                StartTime = block.StartTime,
                EndTime = block.EndTime,
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
