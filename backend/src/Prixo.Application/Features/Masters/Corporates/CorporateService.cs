using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Corporates;

public record CorporateDto(long Id, string Code, string Name, string? ContactPerson, string? Phone,
    string? Email, string? Address, string? GstNumber, decimal CreditLimit, bool IsActive);
public record SaveCorporateRequest(string Code, string Name, string? ContactPerson, string? Phone,
    string? Email, string? Address, string? GstNumber, decimal CreditLimit, bool IsActive = true);

public class SaveCorporateRequestValidator : AbstractValidator<SaveCorporateRequest>
{
    public SaveCorporateRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactPerson).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.GstNumber).MaximumLength(20);
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Corporate clients (global). Schema now; corporate billing settlement is a later phase.</summary>
public class CorporateService(IAppDbContext db)
{
    public async Task<IReadOnlyList<CorporateDto>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var corporates = db.Corporates.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            corporates = corporates.Where(c =>
                EF.Functions.ILike(c.Code, term) || EF.Functions.ILike(c.Name, term));
        }

        return await corporates
            .OrderBy(c => c.Name)
            .Select(c => new CorporateDto(c.Id, c.Code, c.Name, c.ContactPerson, c.Phone,
                c.Email, c.Address, c.GstNumber, c.CreditLimit, c.IsActive))
            .ToListAsync(ct);
    }

    public async Task<long> CreateAsync(SaveCorporateRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(request.Code, null, ct);
        var corporate = new Corporate
        {
            Code = request.Code,
            Name = request.Name,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            GstNumber = request.GstNumber,
            CreditLimit = request.CreditLimit,
            IsActive = request.IsActive,
        };
        db.Corporates.Add(corporate);
        await db.SaveChangesAsync(ct);
        return corporate.Id;
    }

    public async Task UpdateAsync(long id, SaveCorporateRequest request, CancellationToken ct = default)
    {
        var corporate = await db.Corporates.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(Corporate), id);
        await EnsureUniqueCodeAsync(request.Code, id, ct);

        corporate.Code = request.Code;
        corporate.Name = request.Name;
        corporate.ContactPerson = request.ContactPerson;
        corporate.Phone = request.Phone;
        corporate.Email = request.Email;
        corporate.Address = request.Address;
        corporate.GstNumber = request.GstNumber;
        corporate.CreditLimit = request.CreditLimit;
        corporate.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var corporate = await db.Corporates.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(Corporate), id);
        db.Corporates.Remove(corporate); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueCodeAsync(string code, long? selfId, CancellationToken ct)
    {
        if (await db.Corporates.AnyAsync(c => c.Code == code && c.Id != selfId, ct))
        {
            throw new DomainException($"Corporate code '{code}' already exists.");
        }
    }
}
