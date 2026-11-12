using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Icu;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Icu;

/// <summary>Branch-configurable vitals thresholds (plan §15) — hospital-set, not hardcoded, same posture as Tariffs. IcuVitalsService evaluates the active set on every new IpVitals reading.</summary>
public class CriticalAlertRuleService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<CriticalAlertRuleDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.CriticalAlertRules.AsNoTracking()
            .OrderBy(r => r.Parameter)
            .Select(r => new CriticalAlertRuleDto(r.Id, r.Parameter, r.Operator, r.ThresholdValue, r.Severity, r.Description, r.IsActive))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(SaveCriticalAlertRuleRequest request, CancellationToken ct = default)
    {
        var rule = new CriticalAlertRule
        {
            BranchId = RequireBranch(),
            Parameter = request.Parameter,
            Operator = request.Operator,
            ThresholdValue = request.ThresholdValue,
            Severity = request.Severity,
            Description = request.Description,
            IsActive = request.IsActive,
        };
        db.CriticalAlertRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return rule.Id;
    }

    public async Task UpdateAsync(long id, SaveCriticalAlertRuleRequest request, CancellationToken ct = default)
    {
        var rule = await db.CriticalAlertRules.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw new NotFoundException(nameof(CriticalAlertRule), id);
        rule.Parameter = request.Parameter;
        rule.Operator = request.Operator;
        rule.ThresholdValue = request.ThresholdValue;
        rule.Severity = request.Severity;
        rule.Description = request.Description;
        rule.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
