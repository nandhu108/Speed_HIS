using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Billing;

/// <summary>
/// Per-cashier-per-session cash reconciliation, sitting alongside (not instead of)
/// DayCloseService's branch-day gate — see CashierShift's doc comment. Closing a shift only
/// reconciles that shift's own tagged BillPayments; it doesn't touch DayClose, which still
/// gates bill/payment creation for the branch-day as a whole.
/// </summary>
public class CashierShiftService(IAppDbContext db, ICurrentUser currentUser, IActivityLogger activity, TimeProvider timeProvider)
{
    public async Task<long> OpenAsync(OpenCashierShiftRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var userId = currentUser.UserId;
        if (await db.CashierShifts.AnyAsync(s => s.UserId == userId && s.Status == CashierShiftStatus.Open, ct))
        {
            throw new DomainException("You already have an open cashier shift.");
        }

        var shift = new CashierShift
        {
            BranchId = branchId,
            UserId = userId,
            OpenedAt = timeProvider.GetUtcNow(),
            OpeningFloat = request.OpeningFloat,
            Status = CashierShiftStatus.Open,
        };
        db.CashierShifts.Add(shift);
        await db.SaveChangesAsync(ct);
        return shift.Id;
    }

    public async Task<CashierShiftDto?> GetMyOpenShiftAsync(CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        var shift = await db.CashierShifts.AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.ModeAmounts).ThenInclude(m => m.PaymentMode)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == CashierShiftStatus.Open, ct);
        return shift is null ? null : ToDto(shift);
    }

    public async Task<PagedResult<CashierShiftDto>> GetPagedAsync(CashierShiftQuery query, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var shifts = db.CashierShifts.AsNoTracking().Where(s => s.BranchId == branchId);
        if (query.Status is { } status)
        {
            shifts = shifts.Where(s => s.Status == status);
        }

        var totalCount = await shifts.CountAsync(ct);
        var items = await shifts
            .Include(s => s.User)
            .Include(s => s.ReceivedByUser)
            .Include(s => s.ModeAmounts).ThenInclude(m => m.PaymentMode)
            .OrderByDescending(s => s.OpenedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return PagedResult<CashierShiftDto>.Create(items.Select(ToDto).ToList(), query.Page, query.PageSize, totalCount);
    }

    /// <summary>Live system totals for an open shift's tagged payments so far — lets the cashier see what to declare before closing. Same shape as DayCloseService.GetDailyCollectionAsync.</summary>
    public async Task<IReadOnlyList<DailyCollectionModeDto>> GetCollectionAsync(long shiftId, CancellationToken ct = default)
    {
        var shift = await db.CashierShifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == shiftId, ct)
            ?? throw new NotFoundException(nameof(CashierShift), shiftId);
        return await ShiftCollectionAsync(shift, ct);
    }

    /// <summary>
    /// What the cashier should be holding for a shift: payments tagged to the shift, plus deposits
    /// they received during it, minus cash refunds they paid out during it.
    /// </summary>
    private async Task<IReadOnlyList<DailyCollectionModeDto>> ShiftCollectionAsync(CashierShift shift, CancellationToken ct)
    {
        var from = shift.OpenedAt;
        var to = shift.ClosedAt ?? timeProvider.GetUtcNow();
        return await CollectionMath.ComputeAsync(db,
            db.BillPayments.Where(p => p.CashierShiftId == shift.Id),
            db.Advances.Where(a => a.CreatedBy == shift.UserId && a.ReceivedAt >= from && a.ReceivedAt <= to),
            db.Refunds.Where(r => r.Status == RefundStatus.Issued && r.Method == RefundMethod.Cash &&
                r.IssuedByUserId == shift.UserId && r.IssuedAt >= from && r.IssuedAt <= to),
            ct);
    }

    /// <summary>Groups this shift's tagged BillPayments by mode into system amounts, merges in the cashier's declared amounts, and computes variance — same reconciliation shape as DayCloseService.CloseDayAsync.</summary>
    public async Task<CashierShiftDto> CloseAsync(long shiftId, CloseCashierShiftRequest request, CancellationToken ct = default)
    {
        var shift = await db.CashierShifts.Include(s => s.ModeAmounts).Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == shiftId, ct)
            ?? throw new NotFoundException(nameof(CashierShift), shiftId);
        if (shift.UserId != currentUser.UserId)
        {
            throw new DomainException("Only the cashier who opened this shift can close it.");
        }
        if (shift.Status != CashierShiftStatus.Open)
        {
            throw new DomainException($"A shift that is {shift.Status} cannot be closed again.");
        }

        var systemByMode = (await ShiftCollectionAsync(shift, ct)).ToDictionary(m => m.PaymentModeId, m => m.Amount);
        var declaredByMode = request.DeclaredAmounts.ToDictionary(d => d.PaymentModeId, d => d.DeclaredAmount);
        var modeIds = systemByMode.Keys.Union(declaredByMode.Keys).Distinct().ToList();

        foreach (var modeId in modeIds)
        {
            var systemAmount = systemByMode.GetValueOrDefault(modeId);
            var declaredAmount = declaredByMode.GetValueOrDefault(modeId);
            shift.ModeAmounts.Add(new CashierShiftModeAmount
            {
                PaymentModeId = modeId,
                SystemAmount = systemAmount,
                DeclaredAmount = declaredAmount,
                Variance = declaredAmount - systemAmount,
            });
        }

        shift.Status = CashierShiftStatus.Closed;
        shift.ClosedAt = timeProvider.GetUtcNow();
        shift.Notes = request.Notes;

        await activity.LogAsync(ActivityEvents.CashierShiftClosed,
            $"Cashier shift for {shift.User!.FullName} closed", ct: ct);
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(shift.Id, ct);
    }

    /// <summary>A supervisor confirms the physical cash from a closed shift was handed over.</summary>
    public async Task ReceiveAsync(long shiftId, CancellationToken ct = default)
    {
        var shift = await db.CashierShifts.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == shiftId, ct)
            ?? throw new NotFoundException(nameof(CashierShift), shiftId);
        if (shift.Status != CashierShiftStatus.Closed)
        {
            throw new DomainException($"A shift that is {shift.Status} cannot be received.");
        }

        shift.Status = CashierShiftStatus.Received;
        shift.ReceivedByUserId = currentUser.UserId;
        shift.ReceivedAt = timeProvider.GetUtcNow();

        await activity.LogAsync(ActivityEvents.CashierShiftReceived,
            $"Cash received for {shift.User!.FullName}'s cashier shift", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<CashierShiftDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var shift = await db.CashierShifts.AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.ReceivedByUser)
            .Include(s => s.ModeAmounts).ThenInclude(m => m.PaymentMode)
            .FirstOrDefaultAsync(s => s.Id == id, ct) ?? throw new NotFoundException(nameof(CashierShift), id);
        return ToDto(shift);
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");

    private static CashierShiftDto ToDto(CashierShift s) => new(
        s.Id, s.UserId, s.User!.FullName, s.OpenedAt, s.OpeningFloat, s.Status, s.ClosedAt, s.Notes,
        s.ReceivedByUserId, s.ReceivedByUser?.FullName, s.ReceivedAt,
        s.ModeAmounts.Select(m => new CashierShiftModeAmountDto(
            m.PaymentModeId, m.PaymentMode!.Name, m.SystemAmount, m.DeclaredAmount, m.Variance)).ToList());
}
