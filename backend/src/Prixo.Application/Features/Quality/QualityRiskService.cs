using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Quality;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Quality;

/// <summary>Risk Register (plan §20). RiskScore is computed on read via QualityMath, not stored.</summary>
public class QualityRiskService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<long> CreateAsync(SaveRiskRequest request, CancellationToken ct = default)
    {
        var risk = new QualityRisk
        {
            BranchId = RequireBranch(), Title = request.Title, Category = request.Category, Description = request.Description,
            Likelihood = request.Likelihood, Impact = request.Impact, MitigationPlan = request.MitigationPlan,
            OwnerUserId = request.OwnerUserId, IdentifiedDate = request.IdentifiedDate, ReviewDate = request.ReviewDate,
            Status = RiskStatus.Identified,
        };
        db.QualityRisks.Add(risk);
        await db.SaveChangesAsync(ct);
        return risk.Id;
    }

    public async Task<IReadOnlyList<RiskDto>> GetAllAsync(RiskQuery query, CancellationToken ct = default)
    {
        var risks = db.QualityRisks.AsNoTracking().Include(r => r.OwnerUser).AsQueryable();
        if (query.Status is { } status) risks = risks.Where(r => r.Status == status);
        var rows = await risks.OrderByDescending(r => r.IdentifiedDate).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task SetStatusAsync(long id, RiskStatus status, CancellationToken ct = default)
    {
        var risk = await db.QualityRisks.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw new NotFoundException(nameof(QualityRisk), id);
        risk.Status = status;
        if (status == RiskStatus.Closed) risk.ClosedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    private static RiskDto ToDto(QualityRisk r) => new(
        r.Id, r.Title, r.Category, r.Description, r.Likelihood, r.Impact, QualityMath.RiskScore(r.Likelihood, r.Impact),
        r.MitigationPlan, r.OwnerUser!.FullName, r.Status, r.IdentifiedDate, r.ReviewDate, r.ClosedAt);

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
