using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Admin.Branches;

public record BranchDto(long Id, string Code, string Name, string? Address, string? City,
    string? State, string? Pincode, string? Phone, string? Email, string? GstNumber,
    string? ReportHeader, string TimeZone, bool IsActive, string? DefaultLandingPath);
public record SaveBranchRequest(string Code, string Name, string? Address, string? City,
    string? State, string? Pincode, string? Phone, string? Email, string? GstNumber,
    string? ReportHeader, string TimeZone = "Asia/Kolkata", bool IsActive = true,
    string? DefaultLandingPath = null);

public class SaveBranchRequestValidator : AbstractValidator<SaveBranchRequest>
{
    public SaveBranchRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Code must be uppercase letters, digits, underscore or hyphen.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.TimeZone).NotEmpty().MaximumLength(50);
    }
}

public class BranchService(IAppDbContext db)
{
    public async Task<IReadOnlyList<BranchDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Branches.AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => ToDto(b))
            .ToListAsync(ct);

    public async Task<BranchDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new NotFoundException(nameof(Branch), id);
        return ToDto(branch);
    }

    public async Task<long> CreateAsync(SaveBranchRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(request.Code, null, ct);
        var branch = new Branch { Code = request.Code, Name = request.Name };
        Apply(branch, request);
        db.Branches.Add(branch);
        await db.SaveChangesAsync(ct);
        return branch.Id;
    }

    public async Task UpdateAsync(long id, SaveBranchRequest request, CancellationToken ct = default)
    {
        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new NotFoundException(nameof(Branch), id);
        await EnsureUniqueCodeAsync(request.Code, id, ct);

        branch.Code = request.Code;
        branch.Name = request.Name;
        Apply(branch, request);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new NotFoundException(nameof(Branch), id);
        if (await db.UserBranches.AnyAsync(ub => ub.BranchId == id, ct))
        {
            throw new DomainException("Branch has users mapped to it. Remove the mappings first.");
        }

        db.Branches.Remove(branch); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueCodeAsync(string code, long? selfId, CancellationToken ct)
    {
        if (await db.Branches.AnyAsync(b => b.Code == code && b.Id != selfId, ct))
        {
            throw new DomainException($"Branch code '{code}' already exists.");
        }
    }

    private static void Apply(Branch branch, SaveBranchRequest request)
    {
        branch.Address = request.Address;
        branch.City = request.City;
        branch.State = request.State;
        branch.Pincode = request.Pincode;
        branch.Phone = request.Phone;
        branch.Email = request.Email;
        branch.GstNumber = request.GstNumber;
        branch.ReportHeader = request.ReportHeader;
        branch.TimeZone = request.TimeZone;
        branch.IsActive = request.IsActive;
        branch.DefaultLandingPath = request.DefaultLandingPath;
    }

    private static BranchDto ToDto(Branch b) => new(
        b.Id, b.Code, b.Name, b.Address, b.City, b.State, b.Pincode, b.Phone,
        b.Email, b.GstNumber, b.ReportHeader, b.TimeZone, b.IsActive, b.DefaultLandingPath);
}
