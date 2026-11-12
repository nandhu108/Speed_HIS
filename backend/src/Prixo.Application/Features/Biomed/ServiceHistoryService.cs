using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Biomed;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Biomed;

/// <summary>Service History (plan §19) — every completed WorkOrder auto-logs a row (see WorkOrderService.CompleteAsync); this service adds the manual "ad-hoc vendor visit" path plus the read side.</summary>
public class ServiceHistoryService(IAppDbContext db)
{
    public async Task<long> LogAsync(LogServiceHistoryRequest request, CancellationToken ct = default)
    {
        var equipment = await db.BiomedEquipment.FirstOrDefaultAsync(e => e.Id == request.EquipmentId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Biomed.BiomedEquipment), request.EquipmentId);
        var entry = new ServiceHistoryEntry
        {
            BranchId = equipment.BranchId, EquipmentId = request.EquipmentId, ServiceDate = request.ServiceDate,
            ServiceType = request.ServiceType, PerformedBy = request.PerformedBy, Cost = request.Cost, Notes = request.Notes,
        };
        db.ServiceHistoryEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.Id;
    }

    public async Task<IReadOnlyList<ServiceHistoryDto>> GetByEquipmentAsync(long equipmentId, CancellationToken ct = default) =>
        await db.ServiceHistoryEntries.AsNoTracking().Where(s => s.EquipmentId == equipmentId).Include(s => s.WorkOrder)
            .OrderByDescending(s => s.ServiceDate)
            .Select(s => new ServiceHistoryDto(s.Id, s.EquipmentId, s.WorkOrderId, s.WorkOrder != null ? s.WorkOrder.WorkOrderNumber : null, s.ServiceDate, s.ServiceType, s.PerformedBy, s.Cost, s.Notes))
            .ToListAsync(ct);
}
