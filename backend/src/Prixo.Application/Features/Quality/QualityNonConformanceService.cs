using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Quality;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Quality;

/// <summary>Non-conformance findings against an NABH standard (plan §20) — distinct from Incident: an accreditation-gap finding, not a patient/staff-safety event.</summary>
public class QualityNonConformanceService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<long> RaiseAsync(RaiseNonConformanceRequest request, CancellationToken ct = default)
    {
        var nonConformance = new QualityNonConformance
        {
            BranchId = RequireBranch(), StandardId = request.StandardId, DepartmentId = request.DepartmentId,
            Title = request.Title, Description = request.Description, Severity = request.Severity,
            RaisedByUserId = currentUser.UserId, Status = NonConformanceStatus.Open,
        };
        db.QualityNonConformances.Add(nonConformance);
        await db.SaveChangesAsync(ct);
        return nonConformance.Id;
    }

    public async Task<PagedResult<NonConformanceDto>> GetPagedAsync(NonConformanceQuery query, CancellationToken ct = default)
    {
        var items = db.QualityNonConformances.AsNoTracking().AsQueryable();
        if (query.Status is { } status) items = items.Where(n => n.Status == status);

        var totalCount = await items.CountAsync(ct);
        var rows = await items
            .Include(n => n.Standard).Include(n => n.Department).Include(n => n.RaisedByUser).Include(n => n.ClosedByUser)
            .OrderByDescending(n => n.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .ToListAsync(ct);
        return PagedResult<NonConformanceDto>.Create(rows.Select(ToDto).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task MarkInProgressAsync(long id, CancellationToken ct = default)
    {
        var nonConformance = await db.QualityNonConformances.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new NotFoundException(nameof(QualityNonConformance), id);
        if (nonConformance.Status != NonConformanceStatus.Open)
        {
            throw new DomainException($"Only an Open non-conformance can move to In Progress (current: {nonConformance.Status}).");
        }
        nonConformance.Status = NonConformanceStatus.InProgress;
        await db.SaveChangesAsync(ct);
    }

    public async Task CloseAsync(long id, CloseNonConformanceRequest request, CancellationToken ct = default)
    {
        var nonConformance = await db.QualityNonConformances.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new NotFoundException(nameof(QualityNonConformance), id);
        if (nonConformance.Status == NonConformanceStatus.Closed)
        {
            throw new DomainException("Non-conformance is already closed.");
        }
        nonConformance.Status = NonConformanceStatus.Closed;
        nonConformance.ClosedAt = timeProvider.GetUtcNow();
        nonConformance.ClosedByUserId = currentUser.UserId;
        await db.SaveChangesAsync(ct);
    }

    private static NonConformanceDto ToDto(QualityNonConformance n) => new(
        n.Id, n.StandardId, n.Standard?.Code, n.DepartmentId, n.Department?.Name, n.Title, n.Description, n.Severity,
        n.RaisedByUser!.FullName, n.Status, n.ClosedAt, n.ClosedByUser?.FullName);

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
