using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Billing;

public class CreateBillEstimateRequestValidator : AbstractValidator<CreateBillEstimateRequest>
{
    public CreateBillEstimateRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Items).NotEmpty().WithMessage("An estimate needs at least one line.");
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

/// <summary>
/// Pre-admission/pre-procedure cost estimates — a counselling tool, not a bill. Never
/// auto-converts into a real Bill; front desk creates that separately once the patient
/// proceeds. Rate resolution reuses BillMath.ResolveTariffRate, the exact same rule
/// BillingService.AddManualItemAsync uses, so an estimate's numbers track what the patient
/// would actually be billed at today's tariff.
/// </summary>
public class BillEstimateService(
    IAppDbContext db, ICurrentUser currentUser, ISequenceService sequences, IPdfService pdf, TimeProvider timeProvider)
{
    public async Task<PagedResult<BillEstimateListItemDto>> GetPagedAsync(BillEstimateQuery query, CancellationToken ct = default)
    {
        var estimates = db.BillEstimates.AsNoTracking();
        if (query.PatientId is { } patientId)
        {
            estimates = estimates.Where(e => e.PatientId == patientId);
        }

        var totalCount = await estimates.CountAsync(ct);
        var items = await estimates
            .OrderByDescending(e => e.EstimateDate).ThenByDescending(e => e.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new BillEstimateListItemDto(
                e.Id, e.EstimateNumber, e.EstimateDate, e.PatientId, e.Patient!.Uhid,
                e.Patient.FirstName + " " + e.Patient.LastName, e.Status, e.TotalAmount))
            .ToListAsync(ct);

        return PagedResult<BillEstimateListItemDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<BillEstimateDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var estimate = await LoadAsync(id, ct);
        return ToDetailDto(estimate);
    }

    public async Task<long> CreateAsync(CreateBillEstimateRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        if (!await db.Patients.AnyAsync(p => p.Id == request.PatientId, ct))
        {
            throw new NotFoundException(nameof(Patient), request.PatientId);
        }
        if (request.BillEstimateTemplateId is { } templateId && !await db.BillEstimateTemplates.AnyAsync(t => t.Id == templateId, ct))
        {
            throw new NotFoundException(nameof(BillEstimateTemplate), templateId);
        }

        var patientCategoryId = await db.Patients.AsNoTracking()
            .Where(p => p.Id == request.PatientId).Select(p => p.PatientCategoryId).FirstOrDefaultAsync(ct);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var estimate = new BillEstimate
        {
            BranchId = branchId,
            PatientId = request.PatientId,
            BillEstimateTemplateId = request.BillEstimateTemplateId,
            EstimateNumber = string.Empty,
            EstimateDate = today,
            Notes = request.Notes,
        };
        foreach (var line in request.Items)
        {
            estimate.Items.Add(await ToItemAsync(line, branchId, patientCategoryId, today, ct));
        }
        estimate.TotalAmount = estimate.Items.Sum(i => i.TotalAmount);

        var year = today.Year;
        var number = await sequences.NextValueAsync(SequenceKeys.BillEstimateNumber, branchId, year, ct);
        estimate.EstimateNumber = $"EST-{year}-{number.ToString().PadLeft(6, '0')}";

        db.BillEstimates.Add(estimate);
        await db.SaveChangesAsync(ct);
        return estimate.Id;
    }

    /// <summary>Duplicates an estimate's lines into a fresh Draft with a new number — "Bill Estimations Copy".</summary>
    public async Task<long> CopyAsync(long id, CancellationToken ct = default)
    {
        var source = await LoadAsync(id, ct);
        var branchId = RequireBranch();
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var copy = new BillEstimate
        {
            BranchId = branchId,
            PatientId = source.PatientId,
            BillEstimateTemplateId = source.BillEstimateTemplateId,
            EstimateNumber = string.Empty,
            EstimateDate = today,
            Notes = source.Notes,
            TotalAmount = source.TotalAmount,
            Items = source.Items.Select(i => new BillEstimateItem
            {
                SourceType = i.SourceType,
                ServiceId = i.ServiceId,
                PackageId = i.PackageId,
                Description = i.Description,
                Quantity = i.Quantity,
                Rate = i.Rate,
                TotalAmount = i.TotalAmount,
            }).ToList(),
        };

        var year = today.Year;
        var number = await sequences.NextValueAsync(SequenceKeys.BillEstimateNumber, branchId, year, ct);
        copy.EstimateNumber = $"EST-{year}-{number.ToString().PadLeft(6, '0')}";

        db.BillEstimates.Add(copy);
        await db.SaveChangesAsync(ct);
        return copy.Id;
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var estimate = await db.BillEstimates.Include(e => e.Items).FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException(nameof(BillEstimate), id);
        foreach (var item in estimate.Items)
        {
            db.BillEstimateItems.Remove(item);
        }
        db.BillEstimates.Remove(estimate);
        await db.SaveChangesAsync(ct);
    }

    public async Task<byte[]> GeneratePdfAsync(long id, CancellationToken ct = default)
    {
        var estimate = await LoadAsync(id, ct);
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == estimate.BranchId, ct);

        if (estimate.Status == BillEstimateStatus.Draft)
        {
            var tracked = await db.BillEstimates.FirstAsync(e => e.Id == id, ct);
            tracked.Status = BillEstimateStatus.Printed;
            await db.SaveChangesAsync(ct);
        }

        var data = new BillEstimateData(
            estimate.EstimateNumber, estimate.EstimateDate,
            estimate.Patient!.FirstName + " " + estimate.Patient.LastName, estimate.Patient.Uhid,
            branch?.Name ?? string.Empty, branch?.Address ?? string.Empty,
            estimate.Items.Select(i => new BillEstimateLineData(i.Description, i.Quantity, i.Rate, i.TotalAmount)).ToList(),
            estimate.TotalAmount, estimate.Notes);
        return pdf.GenerateBillEstimate(data);
    }

    private async Task<BillEstimateItem> ToItemAsync(
        CreateEstimateItemRequest line, long branchId, long? patientCategoryId, DateOnly asOf, CancellationToken ct)
    {
        string description;
        decimal rate;
        long? serviceId = null;
        long? packageId = null;

        switch (line.SourceType)
        {
            case BillItemSourceType.Service:
                var service = await db.Services.FirstOrDefaultAsync(s => s.Id == line.ServiceId, ct)
                    ?? throw new NotFoundException(nameof(Service), line.ServiceId!.Value);
                var candidates = await db.TariffItems.AsNoTracking()
                    .Where(ti => ti.ServiceId == service.Id && ti.Tariff!.BranchId == branchId)
                    .Select(ti => new TariffCandidate(ti.Tariff!.PatientCategoryId, ti.Tariff!.EffectiveFrom, ti.Tariff!.EffectiveTo, ti.Rate, ti.RoomTypeId))
                    .ToListAsync(ct);
                // No admission context on an estimate line, so room-type-specific rows never
                // apply here (roomTypeId omitted/null) — same match-or-exclude semantics as
                // BillingService.ResolveServiceRateAsync, just always resolving the room-agnostic rate.
                rate = BillMath.ResolveTariffRate(candidates, patientCategoryId, asOf, service.DefaultRate);
                description = service.Name;
                serviceId = service.Id;
                break;
            case BillItemSourceType.Package:
                var package = await db.Packages.FirstOrDefaultAsync(p => p.Id == line.PackageId, ct)
                    ?? throw new NotFoundException(nameof(Package), line.PackageId!.Value);
                rate = package.Price;
                description = package.Name;
                packageId = package.Id;
                break;
            default:
                throw new DomainException("Only Service or Package lines are valid on a bill estimate.");
        }

        return new BillEstimateItem
        {
            SourceType = line.SourceType,
            ServiceId = serviceId,
            PackageId = packageId,
            Description = description,
            Quantity = line.Quantity,
            Rate = rate,
            TotalAmount = rate * line.Quantity,
        };
    }

    private async Task<BillEstimate> LoadAsync(long id, CancellationToken ct) =>
        await db.BillEstimates.AsNoTracking()
            .Include(e => e.Patient)
            .Include(e => e.BillEstimateTemplate)
            .Include(e => e.Items).ThenInclude(i => i.Service)
            .Include(e => e.Items).ThenInclude(i => i.Package)
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException(nameof(BillEstimate), id);

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch before creating an estimate.");

    private static BillEstimateDetailDto ToDetailDto(BillEstimate e) => new(
        e.Id, e.EstimateNumber, e.EstimateDate, e.PatientId, e.Patient!.Uhid, e.Patient.FirstName + " " + e.Patient.LastName,
        e.BillEstimateTemplateId, e.BillEstimateTemplate?.Name, e.Status, e.TotalAmount, e.Notes,
        e.Items.OrderBy(i => i.Id).Select(i => new BillEstimateItemDto(
            i.Id, i.SourceType, i.ServiceId, i.Service?.Name, i.PackageId, i.Package?.Name,
            i.Description, i.Quantity, i.Rate, i.TotalAmount)).ToList());
}
