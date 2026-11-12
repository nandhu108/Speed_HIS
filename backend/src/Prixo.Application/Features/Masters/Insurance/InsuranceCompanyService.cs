using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Insurance;

public record InsuranceCompanyDto(long Id, string Code, string Name, bool IsTpa,
    string? ContactPerson, string? Phone, string? Email, string? Address, bool IsActive);
public record SaveInsuranceCompanyRequest(string Code, string Name, bool IsTpa,
    string? ContactPerson, string? Phone, string? Email, string? Address, bool IsActive = true);

public class SaveInsuranceCompanyRequestValidator : AbstractValidator<SaveInsuranceCompanyRequest>
{
    public SaveInsuranceCompanyRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactPerson).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

/// <summary>
/// Insurance/TPA companies — placeholder master for the held Insurance/TPA module.
/// Schema and CRUD exist now so later activation is configuration, not migration.
/// </summary>
public class InsuranceCompanyService(IAppDbContext db)
{
    public async Task<IReadOnlyList<InsuranceCompanyDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.InsuranceCompanies.AsNoTracking()
            .OrderBy(i => i.Name)
            .Select(i => new InsuranceCompanyDto(i.Id, i.Code, i.Name, i.IsTpa,
                i.ContactPerson, i.Phone, i.Email, i.Address, i.IsActive))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(SaveInsuranceCompanyRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(request.Code, null, ct);
        var company = new InsuranceCompany
        {
            Code = request.Code,
            Name = request.Name,
            IsTpa = request.IsTpa,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            IsActive = request.IsActive,
        };
        db.InsuranceCompanies.Add(company);
        await db.SaveChangesAsync(ct);
        return company.Id;
    }

    public async Task UpdateAsync(long id, SaveInsuranceCompanyRequest request, CancellationToken ct = default)
    {
        var company = await db.InsuranceCompanies.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException(nameof(InsuranceCompany), id);
        await EnsureUniqueCodeAsync(request.Code, id, ct);

        company.Code = request.Code;
        company.Name = request.Name;
        company.IsTpa = request.IsTpa;
        company.ContactPerson = request.ContactPerson;
        company.Phone = request.Phone;
        company.Email = request.Email;
        company.Address = request.Address;
        company.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var company = await db.InsuranceCompanies.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException(nameof(InsuranceCompany), id);
        db.InsuranceCompanies.Remove(company); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueCodeAsync(string code, long? selfId, CancellationToken ct)
    {
        if (await db.InsuranceCompanies.AnyAsync(i => i.Code == code && i.Id != selfId, ct))
        {
            throw new DomainException($"Insurance company code '{code}' already exists.");
        }
    }
}
