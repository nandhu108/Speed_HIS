using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Biomed;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Biomed;

/// <summary>Breakdown Complaint (plan §19: "raise → assign → resolve, downtime capture"). DowntimeMinutes is computed at ResolveAsync via BiomedMath, not user-entered. A formal WorkOrder (with spare usage) can optionally be raised against a complaint via WorkOrderService.CreateFromComplaintAsync.</summary>
public class BreakdownComplaintService(IAppDbContext db, ISequenceService sequences, ICurrentUser currentUser, IActivityLogger activity, TimeProvider timeProvider)
{
    public async Task<long> RaiseAsync(RaiseComplaintRequest request, CancellationToken ct = default)
    {
        var equipment = await db.BiomedEquipment.FirstOrDefaultAsync(e => e.Id == request.EquipmentId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Biomed.BiomedEquipment), request.EquipmentId);

        var now = timeProvider.GetUtcNow();
        var year = now.Year;
        var number = await sequences.NextValueAsync(SequenceKeys.ComplaintNumber, equipment.BranchId, year, ct);
        var complaint = new BreakdownComplaint
        {
            BranchId = equipment.BranchId, EquipmentId = request.EquipmentId, ComplaintNumber = $"BC-{year}-{number.ToString().PadLeft(6, '0')}",
            ReportedByUserId = currentUser.UserId, ReportedAt = now, Description = request.Description,
        };
        db.BreakdownComplaints.Add(complaint);
        equipment.Status = EquipmentStatus.UnderMaintenance;
        await db.SaveChangesAsync(ct);
        return complaint.Id;
    }

    public async Task AssignAsync(long id, AssignComplaintRequest request, CancellationToken ct = default)
    {
        var complaint = await RequireOpenAsync(id, ct);
        complaint.AssignedToUserId = request.AssignedToUserId;
        complaint.AssignedAt = timeProvider.GetUtcNow();
        complaint.Status = ComplaintStatus.Assigned;
        await db.SaveChangesAsync(ct);
    }

    public async Task ResolveAsync(long id, ResolveComplaintRequest request, CancellationToken ct = default)
    {
        var complaint = await db.BreakdownComplaints.Include(c => c.Equipment).FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(BreakdownComplaint), id);
        if (complaint.Status is ComplaintStatus.Resolved or ComplaintStatus.Closed)
        {
            throw new DomainException($"This complaint is {complaint.Status}.");
        }

        var now = timeProvider.GetUtcNow();
        complaint.Status = ComplaintStatus.Resolved;
        complaint.ResolvedAt = now;
        complaint.Resolution = request.Resolution;
        complaint.DowntimeMinutes = BiomedMath.DowntimeMinutes(complaint.ReportedAt, now);
        complaint.Equipment!.Status = EquipmentStatus.InService;

        await activity.LogAsync(ActivityEvents.BiomedComplaintResolved, $"Complaint {complaint.ComplaintNumber} resolved", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task CloseAsync(long id, CancellationToken ct = default)
    {
        var complaint = await db.BreakdownComplaints.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException(nameof(BreakdownComplaint), id);
        if (complaint.Status != ComplaintStatus.Resolved)
        {
            throw new DomainException("Only a resolved complaint can be closed.");
        }
        complaint.Status = ComplaintStatus.Closed;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<BreakdownComplaintDto>> GetAllAsync(ComplaintQuery query, CancellationToken ct = default)
    {
        var complaints = db.BreakdownComplaints.AsNoTracking().Include(c => c.Equipment).Include(c => c.ReportedByUser).Include(c => c.AssignedToUser).AsQueryable();
        if (query.Status is { } status) complaints = complaints.Where(c => c.Status == status);
        if (query.EquipmentId is { } equipmentId) complaints = complaints.Where(c => c.EquipmentId == equipmentId);

        return await complaints.OrderByDescending(c => c.ReportedAt)
            .Select(c => new BreakdownComplaintDto(c.Id, c.ComplaintNumber, c.EquipmentId, c.Equipment!.Name, c.ReportedByUser!.FullName, c.ReportedAt,
                c.Description, c.Status, c.AssignedToUser != null ? c.AssignedToUser.FullName : null, c.AssignedAt, c.ResolvedAt, c.DowntimeMinutes, c.Resolution))
            .ToListAsync(ct);
    }

    private async Task<BreakdownComplaint> RequireOpenAsync(long id, CancellationToken ct)
    {
        var complaint = await db.BreakdownComplaints.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException(nameof(BreakdownComplaint), id);
        if (complaint.Status is ComplaintStatus.Resolved or ComplaintStatus.Closed)
        {
            throw new DomainException($"This complaint is {complaint.Status}.");
        }
        return complaint;
    }
}
