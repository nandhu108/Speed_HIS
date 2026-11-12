using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Quality;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Quality;

/// <summary>
/// CAPA tracker (plan §20: "action, owner, due, verification"). Polymorphic source (SourceType +
/// SourceId, no FK) — a CAPA can be raised against an Incident, a NonConformance, an Audit, or a Risk.
/// </summary>
public class QualityCapaService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<long> RaiseAsync(RaiseCapaRequest request, CancellationToken ct = default)
    {
        var capa = new QualityCapa
        {
            BranchId = RequireBranch(), SourceType = request.SourceType, SourceId = request.SourceId,
            ActionPlan = request.ActionPlan, OwnerUserId = request.OwnerUserId, DueDate = request.DueDate,
            Status = CapaStatus.Open,
        };
        db.QualityCapas.Add(capa);
        await db.SaveChangesAsync(ct);
        return capa.Id;
    }

    public async Task<IReadOnlyList<CapaDto>> GetAllAsync(CapaQuery query, CancellationToken ct = default)
    {
        var capas = db.QualityCapas.AsNoTracking().Include(c => c.OwnerUser).Include(c => c.VerifiedByUser).AsQueryable();
        if (query.Status is { } status) capas = capas.Where(c => c.Status == status);
        if (query.SourceType is { } sourceType) capas = capas.Where(c => c.SourceType == sourceType);
        var rows = await capas.OrderBy(c => c.DueDate).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task CompleteAsync(long id, CancellationToken ct = default)
    {
        var capa = await db.QualityCapas.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException(nameof(QualityCapa), id);
        if (capa.Status is CapaStatus.Verified or CapaStatus.Closed)
        {
            throw new DomainException($"CAPA is already {capa.Status}.");
        }
        capa.Status = CapaStatus.InProgress;
        capa.CompletedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    public async Task VerifyAsync(long id, VerifyCapaRequest request, CancellationToken ct = default)
    {
        var capa = await db.QualityCapas.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException(nameof(QualityCapa), id);
        if (capa.CompletedAt is null)
        {
            throw new DomainException("Mark the CAPA action complete before verifying it.");
        }
        capa.Status = CapaStatus.Verified;
        capa.VerifiedByUserId = currentUser.UserId;
        capa.VerifiedAt = timeProvider.GetUtcNow();
        capa.VerificationNotes = request.VerificationNotes;
        await db.SaveChangesAsync(ct);
    }

    public async Task CloseAsync(long id, CancellationToken ct = default)
    {
        var capa = await db.QualityCapas.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException(nameof(QualityCapa), id);
        if (capa.Status != CapaStatus.Verified)
        {
            throw new DomainException("Only a Verified CAPA can be closed.");
        }
        capa.Status = CapaStatus.Closed;
        await db.SaveChangesAsync(ct);
    }

    private static CapaDto ToDto(QualityCapa c) => new(
        c.Id, c.SourceType, c.SourceId, c.ActionPlan, c.OwnerUser!.FullName, c.DueDate, c.Status,
        c.CompletedAt, c.VerifiedByUser?.FullName, c.VerifiedAt, c.VerificationNotes);

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
