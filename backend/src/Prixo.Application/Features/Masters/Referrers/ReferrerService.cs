using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Referrers;

public record ReferrerDto(long Id, string Code, string Name, ReferrerType Type, long? DoctorId,
    string? DoctorName, string? Phone, string? Organization, bool IsActive);
public record SaveReferrerRequest(string Code, string Name, ReferrerType Type, long? DoctorId,
    string? Phone, string? Organization, bool IsActive = true);

public class SaveReferrerRequestValidator : AbstractValidator<SaveReferrerRequest>
{
    public SaveReferrerRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.DoctorId).NotNull().When(x => x.Type == ReferrerType.Internal)
            .WithMessage("An internal referrer must link to a doctor.");
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Organization).MaximumLength(200);
    }
}

/// <summary>Referral sources (branch-scoped). Internal referrers point at a doctor profile.</summary>
public class ReferrerService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<PagedResult<ReferrerDto>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        var referrers = db.Referrers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            referrers = referrers.Where(r =>
                EF.Functions.ILike(r.Code, term) ||
                EF.Functions.ILike(r.Name, term) ||
                (r.Organization != null && EF.Functions.ILike(r.Organization, term)));
        }

        var totalCount = await referrers.CountAsync(ct);
        var items = await referrers
            .OrderBy(r => r.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new ReferrerDto(r.Id, r.Code, r.Name, r.Type, r.DoctorId,
                r.Doctor != null && r.Doctor.User != null ? r.Doctor.User.FullName : null,
                r.Phone, r.Organization, r.IsActive))
            .ToListAsync(ct);

        return PagedResult<ReferrerDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<long> CreateAsync(SaveReferrerRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesAsync(request, null, ct);
        var referrer = new Referrer
        {
            BranchId = currentUser.BranchId ?? throw new DomainException("A branch context is required."),
            Code = request.Code,
            Name = request.Name,
            Type = request.Type,
            DoctorId = request.Type == ReferrerType.Internal ? request.DoctorId : null,
            Phone = request.Phone,
            Organization = request.Organization,
            IsActive = request.IsActive,
        };
        db.Referrers.Add(referrer);
        await db.SaveChangesAsync(ct);
        return referrer.Id;
    }

    public async Task UpdateAsync(long id, SaveReferrerRequest request, CancellationToken ct = default)
    {
        var referrer = await db.Referrers.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Referrer), id);
        await EnsureReferencesAsync(request, id, ct);

        referrer.Code = request.Code;
        referrer.Name = request.Name;
        referrer.Type = request.Type;
        referrer.DoctorId = request.Type == ReferrerType.Internal ? request.DoctorId : null;
        referrer.Phone = request.Phone;
        referrer.Organization = request.Organization;
        referrer.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var referrer = await db.Referrers.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Referrer), id);
        db.Referrers.Remove(referrer); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureReferencesAsync(SaveReferrerRequest request, long? selfId, CancellationToken ct)
    {
        if (await db.Referrers.AnyAsync(r => r.Code == request.Code && r.Id != selfId, ct))
        {
            throw new DomainException($"Referrer code '{request.Code}' already exists.");
        }
        if (request.Type == ReferrerType.Internal
            && !await db.Doctors.AnyAsync(d => d.Id == request.DoctorId, ct))
        {
            throw new DomainException("Linked doctor does not exist.");
        }
    }
}
