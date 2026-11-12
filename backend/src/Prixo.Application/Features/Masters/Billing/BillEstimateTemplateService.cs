using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Billing;

public record BillEstimateTemplateListItemDto(long Id, string Code, string Name, int ItemCount, bool IsActive);

public record BillEstimateTemplateItemDto(
    long Id, BillItemSourceType SourceType, long? ServiceId, string? ServiceName, long? PackageId,
    string? PackageName, decimal Quantity);

public record BillEstimateTemplateDetailDto(
    long Id, string Code, string Name, string? Description, bool IsActive,
    IReadOnlyList<BillEstimateTemplateItemDto> Items);

public record SaveBillEstimateTemplateItemRequest(BillItemSourceType SourceType, long? ServiceId, long? PackageId, decimal Quantity);

public record SaveBillEstimateTemplateRequest(
    string Code, string Name, string? Description, IReadOnlyList<SaveBillEstimateTemplateItemRequest> Items, bool IsActive = true);

public class SaveBillEstimateTemplateRequestValidator : AbstractValidator<SaveBillEstimateTemplateRequest>
{
    public SaveBillEstimateTemplateRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Items).NotEmpty().WithMessage("A template needs at least one line.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.ServiceId).NotNull()
                .When(i => i.SourceType == BillItemSourceType.Service).WithMessage("Select a service.");
            item.RuleFor(i => i.PackageId).NotNull()
                .When(i => i.SourceType == BillItemSourceType.Package).WithMessage("Select a package.");
        });
    }
}

/// <summary>Reusable Service/Package line sets that seed a pre-admission BillEstimate. Branch-scoped, standard CRUD shaped after PackageService.</summary>
public class BillEstimateTemplateService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<BillEstimateTemplateListItemDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.BillEstimateTemplates.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new BillEstimateTemplateListItemDto(t.Id, t.Code, t.Name, t.Items.Count, t.IsActive))
            .ToListAsync(ct);

    public async Task<BillEstimateTemplateDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var template = await db.BillEstimateTemplates.AsNoTracking()
            .Include(t => t.Items).ThenInclude(i => i.Service)
            .Include(t => t.Items).ThenInclude(i => i.Package)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(BillEstimateTemplate), id);

        return new BillEstimateTemplateDetailDto(template.Id, template.Code, template.Name, template.Description, template.IsActive,
            template.Items.Select(i => new BillEstimateTemplateItemDto(
                i.Id, i.SourceType, i.ServiceId, i.Service?.Name, i.PackageId, i.Package?.Name, i.Quantity)).ToList());
    }

    public async Task<long> CreateAsync(SaveBillEstimateTemplateRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesAsync(request, null, ct);

        var template = new BillEstimateTemplate
        {
            BranchId = RequireBranch(),
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
            Items = request.Items.Select(ToItem).ToList(),
        };
        db.BillEstimateTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return template.Id;
    }

    public async Task UpdateAsync(long id, SaveBillEstimateTemplateRequest request, CancellationToken ct = default)
    {
        var template = await db.BillEstimateTemplates.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(BillEstimateTemplate), id);
        await EnsureReferencesAsync(request, id, ct);

        template.Code = request.Code;
        template.Name = request.Name;
        template.Description = request.Description;
        template.IsActive = request.IsActive;

        foreach (var row in template.Items.ToList())
        {
            db.BillEstimateTemplateItems.Remove(row);
            template.Items.Remove(row);
        }
        foreach (var item in request.Items)
        {
            template.Items.Add(ToItem(item));
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var template = await db.BillEstimateTemplates.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(BillEstimateTemplate), id);

        foreach (var item in template.Items)
        {
            db.BillEstimateTemplateItems.Remove(item);
        }
        db.BillEstimateTemplates.Remove(template);
        await db.SaveChangesAsync(ct);
    }

    private static BillEstimateTemplateItem ToItem(SaveBillEstimateTemplateItemRequest i) => new()
    {
        SourceType = i.SourceType,
        ServiceId = i.SourceType == BillItemSourceType.Service ? i.ServiceId : null,
        PackageId = i.SourceType == BillItemSourceType.Package ? i.PackageId : null,
        Quantity = i.Quantity,
    };

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("A branch context is required.");

    private async Task EnsureReferencesAsync(SaveBillEstimateTemplateRequest request, long? selfId, CancellationToken ct)
    {
        if (await db.BillEstimateTemplates.AnyAsync(t => t.Code == request.Code && t.Id != selfId, ct))
        {
            throw new DomainException($"Bill estimate template code '{request.Code}' already exists.");
        }
        var serviceIds = request.Items.Where(i => i.ServiceId is not null).Select(i => i.ServiceId!.Value).Distinct().ToList();
        if (serviceIds.Count > 0 && await db.Services.CountAsync(s => serviceIds.Contains(s.Id), ct) != serviceIds.Count)
        {
            throw new DomainException("One or more services do not exist.");
        }
        var packageIds = request.Items.Where(i => i.PackageId is not null).Select(i => i.PackageId!.Value).Distinct().ToList();
        if (packageIds.Count > 0 && await db.Packages.CountAsync(p => packageIds.Contains(p.Id), ct) != packageIds.Count)
        {
            throw new DomainException("One or more packages do not exist.");
        }
    }
}
