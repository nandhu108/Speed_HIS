using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Biomed;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Biomed;

/// <summary>PM schedule templates (plan §19: "schedule templates → auto work orders"). GenerateDueAsync is an explicit action, not a background job — same honest scoping call Billing made for bed-charge refresh.</summary>
public class PmScheduleService(IAppDbContext db, WorkOrderService workOrders, TimeProvider timeProvider)
{
    public async Task<long> CreateAsync(SavePmScheduleRequest request, CancellationToken ct = default)
    {
        var equipment = await db.BiomedEquipment.FirstOrDefaultAsync(e => e.Id == request.EquipmentId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Biomed.BiomedEquipment), request.EquipmentId);
        if (request.FrequencyDays <= 0)
        {
            throw new DomainException("Frequency must be a positive number of days.");
        }

        var schedule = new PmSchedule
        {
            BranchId = equipment.BranchId, EquipmentId = request.EquipmentId, FrequencyDays = request.FrequencyDays,
            NextDueDate = request.NextDueDate, ChecklistNotes = request.ChecklistNotes,
        };
        db.PmSchedules.Add(schedule);
        await db.SaveChangesAsync(ct);
        return schedule.Id;
    }

    public async Task<IReadOnlyList<PmScheduleDto>> GetAllAsync(bool dueOnly, CancellationToken ct = default)
    {
        var rows = await db.PmSchedules.AsNoTracking().Include(p => p.Equipment).OrderBy(p => p.NextDueDate).ToListAsync(ct);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var dtos = rows.Select(p => new PmScheduleDto(p.Id, p.EquipmentId, p.Equipment!.Name, p.FrequencyDays, p.NextDueDate,
            p.LastGeneratedDate, BiomedMath.IsPmDue(p.NextDueDate, today), p.ChecklistNotes));
        if (dueOnly) dtos = dtos.Where(d => d.IsDue);
        return dtos.ToList();
    }

    /// <summary>Raises one Open WorkOrder per due schedule that doesn't already have an open/in-progress work order pending.</summary>
    public async Task<int> GenerateDueAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var due = await db.PmSchedules.Where(p => p.NextDueDate <= today).ToListAsync(ct);
        var generated = 0;
        foreach (var schedule in due)
        {
            var hasOpenWorkOrder = await db.WorkOrders.AnyAsync(
                w => w.PmScheduleId == schedule.Id && (w.Status == Domain.Enums.WorkOrderStatus.Open || w.Status == Domain.Enums.WorkOrderStatus.InProgress), ct);
            if (hasOpenWorkOrder) continue;
            await workOrders.CreateFromPmScheduleAsync(schedule, ct);
            generated++;
        }
        return generated;
    }
}
