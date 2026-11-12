using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Pharmacy;
using Prixo.Domain.Entities.Biomed;
using Prixo.Domain.Entities.Pharmacy;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Biomed;

/// <summary>
/// The unit of maintenance work (plan §19) — raised from a due PmSchedule (PmScheduleService)
/// or explicitly from a BreakdownComplaint. Completing one auto-logs a ServiceHistoryEntry
/// and, for a PM work order, rolls the parent schedule's NextDueDate forward. Spare usage
/// decrements stock via the same StockLedgerWriter primitive OT's implant/consumable usage
/// uses — "links Stores" per the plan, no parallel stock-tracking invented.
/// </summary>
public class WorkOrderService(IAppDbContext db, IUnitOfWork unitOfWork, ISequenceService sequences, IActivityLogger activity, TimeProvider timeProvider)
{
    public async Task<long> CreateFromComplaintAsync(CreateWorkOrderFromComplaintRequest request, CancellationToken ct = default)
    {
        var complaint = await db.BreakdownComplaints.FirstOrDefaultAsync(c => c.Id == request.BreakdownComplaintId, ct)
            ?? throw new NotFoundException(nameof(BreakdownComplaint), request.BreakdownComplaintId);
        var workOrder = await CreateCoreAsync(complaint.BranchId, complaint.EquipmentId, WorkOrderType.Breakdown, request.ScheduledDate, request.Notes, ct, breakdownComplaintId: complaint.Id);
        return workOrder.Id;
    }

    internal async Task<WorkOrder> CreateFromPmScheduleAsync(PmSchedule schedule, CancellationToken ct) =>
        await CreateCoreAsync(schedule.BranchId, schedule.EquipmentId, WorkOrderType.PreventiveMaintenance, schedule.NextDueDate, schedule.ChecklistNotes, ct, pmScheduleId: schedule.Id);

    private async Task<WorkOrder> CreateCoreAsync(
        long branchId, long equipmentId, WorkOrderType type, DateOnly scheduledDate, string? notes, CancellationToken ct,
        long? pmScheduleId = null, long? breakdownComplaintId = null)
    {
        var year = scheduledDate.Year;
        var number = await sequences.NextValueAsync(SequenceKeys.WorkOrderNumber, branchId, year, ct);
        var workOrder = new WorkOrder
        {
            BranchId = branchId, EquipmentId = equipmentId, WorkOrderNumber = $"WO-{year}-{number.ToString().PadLeft(6, '0')}",
            WorkOrderType = type, PmScheduleId = pmScheduleId, BreakdownComplaintId = breakdownComplaintId, ScheduledDate = scheduledDate, Notes = notes,
        };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync(ct);
        return workOrder;
    }

    public async Task AssignAsync(long id, AssignWorkOrderRequest request, CancellationToken ct = default)
    {
        var workOrder = await RequireAsync(id, ct);
        RequireOpenOrInProgress(workOrder);
        workOrder.AssignedToUserId = request.AssignedToUserId;
        workOrder.Status = WorkOrderStatus.InProgress;
        await db.SaveChangesAsync(ct);
    }

    public async Task CompleteAsync(long id, CompleteWorkOrderRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var workOrder = await db.WorkOrders.Include(w => w.PmSchedule).Include(w => w.AssignedToUser).FirstOrDefaultAsync(w => w.Id == id, token)
                ?? throw new NotFoundException(nameof(WorkOrder), id);
            RequireOpenOrInProgress(workOrder);
            var now = timeProvider.GetUtcNow();
            var today = DateOnly.FromDateTime(now.UtcDateTime);

            workOrder.Status = WorkOrderStatus.Completed;
            workOrder.CompletedAt = now;
            if (request.Notes is not null) workOrder.Notes = request.Notes;

            db.ServiceHistoryEntries.Add(new ServiceHistoryEntry
            {
                BranchId = workOrder.BranchId, EquipmentId = workOrder.EquipmentId, WorkOrderId = workOrder.Id,
                ServiceDate = today, ServiceType = workOrder.WorkOrderType == WorkOrderType.PreventiveMaintenance ? "Preventive Maintenance" : "Breakdown Repair",
                PerformedBy = workOrder.AssignedToUser?.FullName, Notes = workOrder.Notes,
            });

            if (workOrder.PmSchedule is { } schedule)
            {
                schedule.LastGeneratedDate = today;
                schedule.NextDueDate = today.AddDays(schedule.FrequencyDays);
            }

            await activity.LogAsync(ActivityEvents.BiomedWorkOrderCompleted, $"Work order {workOrder.WorkOrderNumber} completed", ct: token);
            await db.SaveChangesAsync(token);
        }, ct);

    public async Task CancelAsync(long id, CancelWorkOrderRequest request, CancellationToken ct = default)
    {
        var workOrder = await RequireAsync(id, ct);
        RequireOpenOrInProgress(workOrder);
        workOrder.Status = WorkOrderStatus.Cancelled;
        workOrder.Notes = $"{workOrder.Notes}\nCancelled: {request.Reason}".Trim();
        await db.SaveChangesAsync(ct);
    }

    public async Task<long> RecordSpareUsageAsync(long id, RecordSpareUsageRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var workOrder = await RequireAsync(id, token);
            RequireOpenOrInProgress(workOrder);
            var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == request.BatchId, token) ?? throw new NotFoundException(nameof(Batch), request.BatchId);
            if (batch.QuantityOnHand < request.Quantity)
            {
                throw new DomainException("Not enough stock in this batch for the requested quantity.");
            }

            var usage = new SpareUsage { WorkOrderId = id, BatchId = batch.Id, Quantity = request.Quantity };
            db.SpareUsages.Add(usage);
            await db.SaveChangesAsync(token);

            StockLedgerWriter.Write(db, batch, StockMovementType.IssueOut, request.Quantity, nameof(SpareUsage), usage.Id, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(token);
            return usage.Id;
        }, ct);

    public async Task<IReadOnlyList<WorkOrderDto>> GetListAsync(WorkOrderQuery query, CancellationToken ct = default)
    {
        var workOrders = db.WorkOrders.AsNoTracking().Include(w => w.Equipment).Include(w => w.AssignedToUser)
            .Include(w => w.SpareUsages).ThenInclude(s => s.Batch).ThenInclude(b => b!.Drug)
            .Include(w => w.SpareUsages).ThenInclude(s => s.Batch).ThenInclude(b => b!.Item)
            .AsQueryable();
        if (query.Status is { } status) workOrders = workOrders.Where(w => w.Status == status);
        if (query.WorkOrderType is { } type) workOrders = workOrders.Where(w => w.WorkOrderType == type);
        if (query.EquipmentId is { } equipmentId) workOrders = workOrders.Where(w => w.EquipmentId == equipmentId);

        var rows = await workOrders.OrderByDescending(w => w.ScheduledDate).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<WorkOrderDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var w = await db.WorkOrders.AsNoTracking().Include(x => x.Equipment).Include(x => x.AssignedToUser)
            .Include(x => x.SpareUsages).ThenInclude(s => s.Batch).ThenInclude(b => b!.Drug)
            .Include(x => x.SpareUsages).ThenInclude(s => s.Batch).ThenInclude(b => b!.Item)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(WorkOrder), id);
        return ToDto(w);
    }

    private async Task<WorkOrder> RequireAsync(long id, CancellationToken ct) =>
        await db.WorkOrders.FirstOrDefaultAsync(w => w.Id == id, ct) ?? throw new NotFoundException(nameof(WorkOrder), id);

    private static void RequireOpenOrInProgress(WorkOrder workOrder)
    {
        if (workOrder.Status is not (WorkOrderStatus.Open or WorkOrderStatus.InProgress))
        {
            throw new DomainException($"This work order is {workOrder.Status} and cannot be changed.");
        }
    }

    private static WorkOrderDto ToDto(WorkOrder w) => new(
        w.Id, w.WorkOrderNumber, w.EquipmentId, w.Equipment!.Name, w.WorkOrderType, w.PmScheduleId, w.BreakdownComplaintId,
        w.Status, w.ScheduledDate, w.CompletedAt, w.AssignedToUserId, w.AssignedToUser?.FullName, w.Notes,
        w.SpareUsages.Select(s => new SpareUsageDto(s.Id, s.BatchId, ProductNaming.Of(s.Batch!), s.Quantity)).ToList());
}
