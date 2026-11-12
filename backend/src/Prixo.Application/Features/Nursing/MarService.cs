using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Pharmacy;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Nursing;
using Prixo.Domain.Entities.Pharmacy;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Nursing;

/// <summary>
/// Medication Administration Record (plan §10). GetAsync lazily materializes the day's
/// MarSchedule rows from active MedicationOrders the first time it's asked for that
/// admission/date — no background job, same "generate on read" posture as nothing else in
/// this codebase pre-computes but is cheap and idempotent (skips due times already generated).
/// ActionAsync is the single administer/skip/refuse endpoint; high-alert drugs require a
/// second, different user's VerifiedByUserId (double-verification). An Administered dose that
/// also names a Batch/Quantity decrements Pharmacy stock in the same transaction (closing the
/// "MAR never touches inventory" gap) — the batch link is optional, so charting a dose is never
/// blocked by a stock problem the nurse didn't choose to report.
/// </summary>
public class MarService(IAppDbContext db, IUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<MarScheduleDto>> GetAsync(long admissionId, DateOnly date, CancellationToken ct = default)
    {
        if (!await db.Admissions.AnyAsync(a => a.Id == admissionId, ct))
        {
            throw new NotFoundException(nameof(Admission), admissionId);
        }
        await GenerateForDateAsync(admissionId, date, ct);

        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);
        var rows = await db.MarSchedules.AsNoTracking().Where(m => m.AdmissionId == admissionId && m.DueAt >= dayStart && m.DueAt < dayEnd)
            .Include(m => m.MedicationOrder).ThenInclude(o => o!.Drug)
            .Include(m => m.Administration).ThenInclude(a => a!.ActionByUser)
            .Include(m => m.Administration).ThenInclude(a => a!.VerifiedByUser)
            .Include(m => m.Administration).ThenInclude(a => a!.Batch)
            .OrderBy(m => m.DueAt)
            .ToListAsync(ct);

        return [.. rows.Select(m => new MarScheduleDto(
            m.Id, m.MedicationOrderId, m.MedicationOrder!.DrugId, m.MedicationOrder.Drug!.Name, m.MedicationOrder.Dose,
            m.MedicationOrder.Route, m.MedicationOrder.Frequency, m.DueAt, m.Status, m.IsHighAlert,
            m.Administration is { } admin
                ? new MarAdministrationDto(admin.Action, admin.ActionAt, admin.ActionByUser!.FullName, admin.VerifiedByUser?.FullName, admin.Reason, admin.Notes,
                    admin.BatchId, admin.Batch?.BatchNumber, admin.Quantity, admin.BatchId is not null)
                : null))];
    }

    public async Task ActionAsync(long marScheduleId, MarActionRequest request, CancellationToken ct = default)
    {
        var schedule = await db.MarSchedules.FirstOrDefaultAsync(m => m.Id == marScheduleId, ct)
            ?? throw new NotFoundException(nameof(MarSchedule), marScheduleId);
        if (schedule.Status != MarStatus.Pending)
        {
            throw new DomainException($"This dose is already {schedule.Status} and cannot be actioned again.");
        }
        if (request.Action == MarStatus.Pending)
        {
            throw new DomainException("Action must be Administered, Skipped or Refused.");
        }
        if (request.Action != MarStatus.Administered && string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new DomainException("A reason is required to skip or refuse a dose.");
        }
        if (schedule.IsHighAlert && request.Action == MarStatus.Administered)
        {
            if (request.VerifiedByUserId is null)
            {
                throw new DomainException("High-alert drugs require a second nurse's double-verification before administration.");
            }
            if (request.VerifiedByUserId == currentUser.UserId)
            {
                throw new DomainException("Double-verification requires a different user than the one administering.");
            }
            if (!await db.Users.AnyAsync(u => u.Id == request.VerifiedByUserId, ct))
            {
                throw new NotFoundException("User", request.VerifiedByUserId.Value);
            }
        }

        Batch? batch = null;
        if (request.BatchId is { } batchId)
        {
            batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == batchId, ct)
                ?? throw new NotFoundException(nameof(Batch), batchId);
            if (batch.QuantityOnHand < request.Quantity!.Value)
            {
                throw new DomainException($"Batch {batch.BatchNumber} only has {batch.QuantityOnHand} on hand.");
            }
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var now = timeProvider.GetUtcNow();
            schedule.Status = request.Action;
            var administration = new MarAdministration
            {
                MarScheduleId = marScheduleId, Action = request.Action, ActionAt = now, ActionByUserId = currentUser.UserId,
                VerifiedByUserId = request.VerifiedByUserId, Reason = request.Reason, Notes = request.Notes,
                BatchId = request.BatchId, Quantity = request.BatchId is not null ? request.Quantity : null,
            };
            db.MarAdministrations.Add(administration);
            await db.SaveChangesAsync(token);

            if (batch is not null)
            {
                StockLedgerWriter.Write(db, batch, StockMovementType.IssueOut, request.Quantity!.Value, nameof(MarAdministration), administration.Id, now);
                await db.SaveChangesAsync(token);
            }
        }, ct);
    }

    private async Task GenerateForDateAsync(long admissionId, DateOnly date, CancellationToken ct)
    {
        var orders = await db.DoctorOrders.AsNoTracking()
            .Where(o => o.AdmissionId == admissionId && o.OrderType == DoctorOrderType.Medication && o.Status != ClinicalOrderStatus.Cancelled)
            .Include(o => o.MedicationOrder).ThenInclude(m => m!.Drug)
            .ToListAsync(ct);

        var existingDueAts = (await db.MarSchedules.AsNoTracking()
            .Where(m => m.AdmissionId == admissionId)
            .Select(m => new { m.MedicationOrderId, m.DueAt })
            .ToListAsync(ct))
            .ToHashSet();

        var newRows = new List<MarSchedule>();
        foreach (var order in orders)
        {
            var med = order.MedicationOrder!;
            var dueTimes = NursingMath.GenerateDueTimes(med.Frequency, med.StartDate, med.EndDate, date, order.OrderedAt);
            foreach (var dueAt in dueTimes)
            {
                if (existingDueAts.Contains(new { MedicationOrderId = med.Id, DueAt = dueAt }))
                {
                    continue;
                }
                newRows.Add(new MarSchedule
                {
                    AdmissionId = admissionId, MedicationOrderId = med.Id, DueAt = dueAt,
                    Status = MarStatus.Pending, IsHighAlert = med.Drug!.IsHighAlert,
                });
            }
        }
        if (newRows.Count > 0)
        {
            db.MarSchedules.AddRange(newRows);
            await db.SaveChangesAsync(ct);
        }
    }
}
