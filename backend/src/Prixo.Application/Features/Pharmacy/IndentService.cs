using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Pharmacy;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Pharmacy;

/// <summary>
/// A department's "indent → approve → issue" request against a store (plan §13) — sits in
/// front of the existing Issue engine rather than duplicating it. Approving doesn't move
/// stock; it just sets ApprovedQuantity per line so IssueService can validate against it
/// when someone later creates an Issue with IndentId set.
/// </summary>
public class IndentService(
    IAppDbContext db, ISequenceService sequences, ICurrentUser currentUser, IActivityLogger activity, TimeProvider timeProvider)
{
    public async Task<long> CreateAsync(CreateIndentRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        if (!await db.Stores.AnyAsync(s => s.Id == request.StoreId, ct))
        {
            throw new NotFoundException(nameof(Domain.Entities.Pharmacy.Store), request.StoreId);
        }
        if (!await db.Departments.AnyAsync(d => d.Id == request.DepartmentId, ct))
        {
            throw new NotFoundException(nameof(Domain.Entities.Masters.Department), request.DepartmentId);
        }

        var today = Today();
        var number = await sequences.NextValueAsync(SequenceKeys.IndentNumber, branchId, today.Year, ct);
        var indent = new Indent
        {
            BranchId = branchId,
            StoreId = request.StoreId,
            DepartmentId = request.DepartmentId,
            IndentNumber = $"IND-{today.Year}-{number.ToString().PadLeft(6, '0')}",
            IndentDate = today,
            RequestedByUserId = currentUser.UserId,
            Notes = request.Notes,
            Items = request.Items.Select(i => new IndentItem
            {
                DrugId = i.DrugId,
                ItemId = i.ItemId,
                RequestedQuantity = i.RequestedQuantity,
            }).ToList(),
        };
        db.Indents.Add(indent);
        await activity.LogAsync(ActivityEvents.IndentRequested, $"Indent {indent.IndentNumber} requested", ct: ct);
        await db.SaveChangesAsync(ct);
        return indent.Id;
    }

    public async Task<IReadOnlyList<IndentListItemDto>> GetPagedAsync(IndentQuery query, CancellationToken ct = default)
    {
        var indents = db.Indents.AsNoTracking();
        if (query.Status is { } status) indents = indents.Where(i => i.Status == status);
        return await indents.Include(i => i.Store).Include(i => i.Department)
            .OrderByDescending(i => i.IndentDate).ThenByDescending(i => i.Id)
            .Select(i => new IndentListItemDto(i.Id, i.IndentNumber, i.IndentDate, i.Store!.Name, i.Department!.Name, i.Status, i.Items.Count))
            .ToListAsync(ct);
    }

    public async Task<IndentDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var i = await db.Indents.AsNoTracking().Include(x => x.Store).Include(x => x.Department)
            .Include(x => x.Items).ThenInclude(x => x.Drug)
            .Include(x => x.Items).ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(Indent), id);
        return new IndentDetailDto(i.Id, i.IndentNumber, i.IndentDate, i.StoreId, i.Store!.Name, i.DepartmentId, i.Department!.Name,
            i.Status, i.Notes, i.RejectionReason,
            i.Items.Select(x => new IndentItemDto(x.Id, x.DrugId, x.ItemId, x.Drug?.Name ?? x.Item?.Name ?? "", x.RequestedQuantity, x.ApprovedQuantity)).ToList());
    }

    /// <summary>Approving may trim ApprovedQuantity per line (defaults to RequestedQuantity); rejecting requires a reason.</summary>
    public async Task DecideAsync(long id, DecideIndentRequest request, CancellationToken ct = default)
    {
        var indent = await db.Indents.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(Indent), id);
        if (indent.Status != IndentStatus.Requested)
        {
            throw new DomainException($"An indent that is {indent.Status} cannot be decided again.");
        }

        if (request.Approve)
        {
            foreach (var item in indent.Items)
            {
                var decided = request.Items?.FirstOrDefault(x => x.IndentItemId == item.Id);
                item.ApprovedQuantity = decided?.ApprovedQuantity ?? item.RequestedQuantity;
            }
            indent.Status = IndentStatus.Approved;
            await activity.LogAsync(ActivityEvents.IndentApproved, $"Indent {indent.IndentNumber} approved", ct: ct);
        }
        else
        {
            indent.Status = IndentStatus.Rejected;
            indent.RejectionReason = request.RejectionReason;
            await activity.LogAsync(ActivityEvents.IndentRejected, $"Indent {indent.IndentNumber} rejected: {request.RejectionReason}", ct: ct);
        }
        indent.DecidedByUserId = currentUser.UserId;
        indent.DecidedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
