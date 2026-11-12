using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Billing;

public record DoctorPayoutRuleDto(
    long Id, long DoctorId, string DoctorName, long ServiceCategoryId, string ServiceCategoryName,
    PayoutType PayoutType, decimal Value, bool IsActive);

public record SaveDoctorPayoutRuleRequest(
    long DoctorId, long ServiceCategoryId, PayoutType PayoutType, decimal Value, bool IsActive = true);

public class SaveDoctorPayoutRuleRequestValidator : AbstractValidator<SaveDoctorPayoutRuleRequest>
{
    public SaveDoctorPayoutRuleRequestValidator()
    {
        RuleFor(x => x.DoctorId).GreaterThan(0);
        RuleFor(x => x.ServiceCategoryId).GreaterThan(0);
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Value).LessThanOrEqualTo(100).When(x => x.PayoutType == PayoutType.Percent)
            .WithMessage("A percent payout cannot exceed 100.");
    }
}

/// <summary>Doctor fee/commission-share definitions per service category. Branch-scoped, standard CRUD. Backs the Doctors Payout / Performing Doctor reports.</summary>
public class DoctorPayoutRuleService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<DoctorPayoutRuleDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.DoctorPayoutRules.AsNoTracking()
            .Include(r => r.Doctor).ThenInclude(d => d!.User)
            .Include(r => r.ServiceCategory)
            .OrderBy(r => r.Doctor!.User!.FullName).ThenBy(r => r.ServiceCategory!.Name)
            .Select(r => ToDto(r))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(SaveDoctorPayoutRuleRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesAsync(request, null, ct);

        var rule = new DoctorPayoutRule
        {
            BranchId = RequireBranch(),
            DoctorId = request.DoctorId,
            ServiceCategoryId = request.ServiceCategoryId,
            PayoutType = request.PayoutType,
            Value = request.Value,
            IsActive = request.IsActive,
        };
        db.DoctorPayoutRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return rule.Id;
    }

    public async Task UpdateAsync(long id, SaveDoctorPayoutRuleRequest request, CancellationToken ct = default)
    {
        var rule = await db.DoctorPayoutRules.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(DoctorPayoutRule), id);
        await EnsureReferencesAsync(request, id, ct);

        rule.DoctorId = request.DoctorId;
        rule.ServiceCategoryId = request.ServiceCategoryId;
        rule.PayoutType = request.PayoutType;
        rule.Value = request.Value;
        rule.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var rule = await db.DoctorPayoutRules.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(DoctorPayoutRule), id);
        db.DoctorPayoutRules.Remove(rule);
        await db.SaveChangesAsync(ct);
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("A branch context is required.");

    private async Task EnsureReferencesAsync(SaveDoctorPayoutRuleRequest request, long? selfId, CancellationToken ct)
    {
        if (!await db.Doctors.AnyAsync(d => d.Id == request.DoctorId, ct))
        {
            throw new NotFoundException(nameof(Doctor), request.DoctorId);
        }
        if (!await db.ServiceCategories.AnyAsync(c => c.Id == request.ServiceCategoryId, ct))
        {
            throw new NotFoundException(nameof(ServiceCategory), request.ServiceCategoryId);
        }
        if (await db.DoctorPayoutRules.AnyAsync(r =>
                r.DoctorId == request.DoctorId && r.ServiceCategoryId == request.ServiceCategoryId && r.Id != selfId, ct))
        {
            throw new DomainException("A payout rule already exists for this doctor and service category.");
        }
    }

    private static DoctorPayoutRuleDto ToDto(DoctorPayoutRule r) => new(
        r.Id, r.DoctorId, r.Doctor!.User!.FullName, r.ServiceCategoryId, r.ServiceCategory!.Name, r.PayoutType, r.Value, r.IsActive);
}
