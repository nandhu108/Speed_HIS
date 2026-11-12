using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Vendors;

public record VendorDto(long Id, string Code, string Name, string? ContactPerson, string? Phone,
    string? Email, string? Address, string? GstNumber, bool IsActive);
public record SaveVendorRequest(string Code, string Name, string? ContactPerson, string? Phone,
    string? Email, string? Address, string? GstNumber, bool IsActive = true);

public class SaveVendorRequestValidator : AbstractValidator<SaveVendorRequest>
{
    public SaveVendorRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactPerson).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.GstNumber).MaximumLength(20);
    }
}

/// <summary>Supplier registry, branch-scoped. Shared later by pharmacy purchases and general stores.</summary>
public class VendorService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<PagedResult<VendorDto>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        var vendors = db.Vendors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            vendors = vendors.Where(v =>
                EF.Functions.ILike(v.Code, term) ||
                EF.Functions.ILike(v.Name, term) ||
                (v.Phone != null && EF.Functions.ILike(v.Phone, term)));
        }

        var totalCount = await vendors.CountAsync(ct);
        var items = await vendors
            .OrderBy(v => v.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(v => new VendorDto(v.Id, v.Code, v.Name, v.ContactPerson, v.Phone,
                v.Email, v.Address, v.GstNumber, v.IsActive))
            .ToListAsync(ct);

        return PagedResult<VendorDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<long> CreateAsync(SaveVendorRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(request.Code, null, ct);
        var vendor = new Vendor
        {
            BranchId = currentUser.BranchId ?? throw new DomainException("A branch context is required."),
            Code = request.Code,
            Name = request.Name,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            GstNumber = request.GstNumber,
            IsActive = request.IsActive,
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync(ct);
        return vendor.Id;
    }

    public async Task UpdateAsync(long id, SaveVendorRequest request, CancellationToken ct = default)
    {
        var vendor = await db.Vendors.FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new NotFoundException(nameof(Vendor), id);
        await EnsureUniqueCodeAsync(request.Code, id, ct);

        vendor.Code = request.Code;
        vendor.Name = request.Name;
        vendor.ContactPerson = request.ContactPerson;
        vendor.Phone = request.Phone;
        vendor.Email = request.Email;
        vendor.Address = request.Address;
        vendor.GstNumber = request.GstNumber;
        vendor.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var vendor = await db.Vendors.FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new NotFoundException(nameof(Vendor), id);
        db.Vendors.Remove(vendor); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueCodeAsync(string code, long? selfId, CancellationToken ct)
    {
        if (await db.Vendors.AnyAsync(v => v.Code == code && v.Id != selfId, ct))
        {
            throw new DomainException($"Vendor code '{code}' already exists.");
        }
    }
}
